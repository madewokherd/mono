/**
 * \file
 * Interface to the dynamic linker
 *
 * Author:
 *    Mono Team (http://www.mono-project.com)
 *
 * Copyright 2001-2004 Ximian, Inc.
 * Copyright 2004-2009 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "config.h"
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-dl.h"
#include "mono/utils/mono-embed.h"
#include "mono/utils/mono-path.h"
#include "mono/utils/mono-threads-api.h"

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>

// Contains LIBC_SO definition
#ifdef HAVE_GNU_LIB_NAMES_H
#include <gnu/lib-names.h>
#endif

struct MonoDlFallbackHandler {
	MonoDlFallbackLoad load_func;
	MonoDlFallbackSymbol symbol_func;
	MonoDlFallbackClose close_func;
	void *user_data;
};

static GSList *fallback_handlers;

#if defined (_AIX)
#include <ar.h>
#include <fcntl.h>

/**
 * On AIX/PASE, a shared library can be contained inside of an ar format
 * archive. Determine if the file is an ar archive or not.
 */
static gboolean
is_library_ar_archive (char *path)
{
	int lfd, readret;
	char magic [SAIAMAG];
	lfd = open (path, O_RDONLY);

	/* don't assume it's an archive on error */
	if (lfd == -1)
		return FALSE;

	readret = read (lfd, magic, SAIAMAG);
	close (lfd);
	/* check for equality with either version of header */
	return readret == SAIAMAG &&
		(memcmp (magic, AIAMAG, SAIAMAG) == 0 ||
		 memcmp (magic, AIAMAGBIG, SAIAMAG) == 0);
}
#endif

/*
 * read a value string from line with any of the following formats:
 * \s*=\s*'string'
 * \s*=\s*"string"
 * \s*=\s*non_white_space_string
 */
static char*
read_string (char *p, FILE *file)
{
	char *endp;
	char *startp;
	while (*p && isspace (*p))
		++p;
	if (*p == 0)
		return NULL;
	if (*p == '=')
		p++;
	while (*p && isspace (*p))
		++p;
	if (*p == '\'' || *p == '"') {
		char t = *p;
		p++;
		startp = p;
		endp = strchr (p, t);
		/* FIXME: may need to read more from file... */
		if (!endp)
			return NULL;
		*endp = 0;
		return (char *) g_memdup (startp, (endp - startp) + 1);
	}
	if (*p == 0)
		return NULL;
	startp = p;
	while (*p && !isspace (*p))
		++p;
	*p = 0;
	return (char *) g_memdup (startp, (p - startp) + 1);
}

/*
 * parse a libtool .la file and return the path of the file to dlopen ()
 * handling both the installed and uninstalled cases
 */
static char*
get_dl_name_from_libtool (const char *libtool_file)
{
	FILE* file;
	char buf [512];
	char *line, *dlname = NULL, *libdir = NULL, *installed = NULL;
	if (!(file = fopen (libtool_file, "r")))
		return NULL;
	while ((line = fgets (buf, 512, file))) {
		while (*line && isspace (*line))
			++line;
		if (*line == '#' || *line == 0)
			continue;
		if (strncmp ("dlname", line, 6) == 0) {
			g_free (dlname);
			dlname = read_string (line + 6, file);
		} else if (strncmp ("libdir", line, 6) == 0) {
			g_free (libdir);
			libdir = read_string (line + 6, file);
		} else if (strncmp ("installed", line, 9) == 0) {
			g_free (installed);
			installed = read_string (line + 9, file);
		}
	}
	fclose (file);
	line = NULL;
	if (installed && strcmp (installed, "no") == 0) {
		char *dir = g_path_get_dirname (libtool_file);
		if (dlname)
			line = g_strconcat (dir, G_DIR_SEPARATOR_S ".libs" G_DIR_SEPARATOR_S, dlname, (const char*)NULL);
		g_free (dir);
	} else {
		if (libdir && dlname)
			line = g_strconcat (libdir, G_DIR_SEPARATOR_S, dlname, (const char*)NULL);
	}
	g_free (dlname);
	g_free (libdir);
	g_free (installed);
	return line;
}


/**
 * mono_dl_open_self:
 * \param error_msg pointer for error message on failure
 *
 * Returns a handle to the main program, on android x86 it's not possible to 
 * call dl_open(null), it returns a null handle, so this function returns RTLD_DEFAULT
 * handle in this platform.
 */
MonoDl*
mono_dl_open_self (char **error_msg)
{
	return mono_dl_open (NULL, MONO_DL_LAZY, error_msg);
}

/**
 * mono_dl_open:
 * \param name name of file containing shared module
 * \param flags flags
 * \param error_msg pointer for error message on failure
 *
 * Load the given file \p name as a shared library or dynamically loadable
 * module. \p name can be NULL to indicate loading the currently executing
 * binary image.
 * \p flags can have the \c MONO_DL_LOCAL bit set to avoid exporting symbols
 * from the module to the shared namespace. The \c MONO_DL_LAZY bit can be set
 * to lazily load the symbols instead of resolving everything at load time.
 * \p error_msg points to a string where an error message will be stored in
 * case of failure.   The error must be released with \c g_free.
 * \returns a \c MonoDl pointer on success, NULL on failure.
 */
MonoDl*
mono_dl_open (const char *name, int flags, char **error_msg)
{
#ifdef HOST_WIN32
	const int native_flags = LOAD_WITH_ALTERED_SEARCH_PATH;
#else
	const int native_flags = 0;
#endif
	return mono_dl_open_full (name, flags, native_flags, error_msg);
}

MonoDl *
mono_dl_open_full (const char *name, int mono_flags, int native_flags, char **error_msg)
{
	MonoDl *module;
	void *lib;
	MonoDlFallbackHandler *dl_fallback = NULL;
	int lflags = mono_dl_convert_flags (mono_flags, native_flags);
	char *found_name;

	if (error_msg)
		*error_msg = NULL;

	module = (MonoDl *) g_malloc (sizeof (MonoDl));
	if (!module) {
		if (error_msg)
			*error_msg = g_strdup ("Out of memory");
		return NULL;
	}
	module->main_module = name == NULL? TRUE: FALSE;


	// No GC safe transition because this is called early in main.c
	lib = mono_dl_open_file (name, lflags);
	if (lib)
		found_name = g_strdup (name);

	if (!lib) {
		GSList *node;
		for (node = fallback_handlers; node != NULL; node = node->next){
			MonoDlFallbackHandler *handler = (MonoDlFallbackHandler *) node->data;
			if (error_msg)
				*error_msg = NULL;
			
			lib = handler->load_func (name, lflags, error_msg, handler->user_data);
			if (error_msg && *error_msg != NULL)
				g_free (*error_msg);
			
			if (lib != NULL){
				dl_fallback = handler;
				found_name = g_strdup (name);
				break;
			}
		}
	}
	if (!lib && !dl_fallback) {
		char *lname;
		char *llname;
		const char *suff;
		const char *ext;
		/* This platform does not support dlopen */
		if (name == NULL) {
			g_free (module);
			return NULL;
		}
		
		suff = ".la";
		ext = strrchr (name, '.');
		if (ext && strcmp (ext, ".la") == 0)
			suff = "";
		lname = g_strconcat (name, suff, (const char*)NULL);
		llname = get_dl_name_from_libtool (lname);
		g_free (lname);
		if (llname) {
			lib = mono_dl_open_file (llname, lflags);
			if (lib)
				found_name = g_strdup (llname);
#if defined (_AIX)
			/*
			 * HACK: deal with AIX archive members because libtool
			 * underspecifies when using --with-aix-soname=svr4 -
			 * without this check, Mono can't find System.Native
			 * at build time.
			 * XXX: Does this also need to be in other places?
			 */
			if (!lib && is_library_ar_archive (llname)) {
				/* try common suffix */
				char *llaixname;
				llaixname = g_strconcat (llname, "(shr_64.o)", (const char*)NULL);
				lib = mono_dl_open_file (llaixname, lflags);
				if (lib)
					found_name = g_strdup (llaixname);
				/* XXX: try another suffix like (shr.o)? */
				g_free (llaixname);
			}
#endif
			g_free (llname);
		}
		if (!lib) {
			if (error_msg) {
				*error_msg = mono_dl_current_error_string ();
			}
			g_free (module);
			return NULL;
		}
	}
	mono_refcount_init (module, NULL);
	module->handle = lib;
	module->dl_fallback = dl_fallback;
	module->full_name = found_name;
	return module;
}

/**
 * mono_dl_symbol:
 * \param module a MonoDl pointer
 * \param name symbol name
 * \param symbol pointer for the result value
 * Load the address of symbol \p name from the given \p module.
 * The address is stored in the pointer pointed to by \p symbol.
 * \returns NULL on success, an error message on failure
 */
char*
mono_dl_symbol (MonoDl *module, const char *name, void **symbol)
{
	void *sym;
	char *err = NULL;

	if (module->dl_fallback) {
		sym = module->dl_fallback->symbol_func (module->handle, name, &err, module->dl_fallback->user_data);
	} else {
#if MONO_DL_NEED_USCORE
		{
			const size_t length = strlen (name);
			char *usname = g_new (char, length + 2);
			*usname = '_';
			memcpy (usname + 1, name, length + 1);
			sym = mono_dl_lookup_symbol (module, usname);
			g_free (usname);
		}
#else
		sym = mono_dl_lookup_symbol (module, name);
#endif
	}

	if (sym) {
		if (symbol)
			*symbol = sym;
		return NULL;
	}
	if (symbol)
		*symbol = NULL;
	return (module->dl_fallback != NULL) ? err :  mono_dl_current_error_string ();
}

/**
 * mono_dl_close:
 * \param module a \c MonoDl pointer
 * Unload the given module and free the module memory.
 * \returns \c 0 on success.
 */
void
mono_dl_close (MonoDl *module)
{
	MonoDlFallbackHandler *dl_fallback = module->dl_fallback;
	
	if (dl_fallback){
		if (dl_fallback->close_func != NULL)
			dl_fallback->close_func (module->handle, dl_fallback->user_data);
	} else
		mono_dl_close_handle (module);
	
	g_free (module->full_name);
	g_free (module);
}

/**
 * mono_dl_build_path:
 * \param directory optional directory
 * \param name base name of the library
 * \param iter iterator token
 * Given a directory name and the base name of a library, iterate
 * over the possible file names of the library, taking into account
 * the possible different suffixes and prefixes on the host platform.
 *
 * The returned file name must be freed by the caller.
 * \p iter must point to a NULL pointer the first time the function is called
 * and then passed unchanged to the following calls.
 * \returns the filename or NULL at the end of the iteration
 */
char*
mono_dl_build_path (const char *directory, const char *name, void **iter)
{
	int idx;
	const char *prefix;
	const char *suffix;
	gboolean need_prefix = TRUE, need_suffix = TRUE;
	int prlen;
	int suffixlen;
	char *res;
	int iteration;

	if (!iter)
		return NULL;

	/*
	  The first time we are called, idx = 0 (as *iter is initialized to NULL). This is our
	  "bootstrap" phase in which we check the passed name verbatim and only if we fail to find
	  the dll thus named, we start appending suffixes, each time increasing idx twice (since now
	  the 0 value became special and we need to offset idx to a 0-based array index). This is
	  done to handle situations when mapped dll name is specified as libsomething.so.1 or
	  libsomething.so.1.1 or libsomething.so - testing it algorithmically would be an overkill
	  here.
	 */
	iteration = GPOINTER_TO_UINT (*iter);
	idx = iteration;
	if (idx == 0) {
		/* Name */
		need_prefix = FALSE;
		need_suffix = FALSE;
		suffix = "";
	} else if (idx == 1) {
		suffix = mono_dl_get_so_suffixes () [idx - 1];
		if (suffix [0] == '\0')
			return NULL;
	} else {
		/* Prefix.Name.suffix */
		suffix = mono_dl_get_so_suffixes () [idx - 2];
		if (suffix [0] == '\0')
			return NULL;
	}

	if (need_prefix) {
		prlen = strlen (mono_dl_get_so_prefix ());
		if (prlen && strncmp (name, mono_dl_get_so_prefix (), prlen) != 0)
			prefix = mono_dl_get_so_prefix ();
		else
			prefix = "";
	} else {
		prefix = "";
	}

	suffixlen = strlen (suffix);
	if (need_suffix && (suffixlen && strstr (name, suffix) == (name + strlen (name) - suffixlen)))
		suffix = "";

	if (directory && *directory)
		res = g_strconcat (directory, G_DIR_SEPARATOR_S, prefix, name, suffix, (const char*)NULL);
	else
		res = g_strconcat (prefix, name, suffix, (const char*)NULL);
	++iteration;
	*iter = GUINT_TO_POINTER (iteration);
	return res;
}

MonoDlFallbackHandler *
mono_dl_fallback_register (MonoDlFallbackLoad load_func, MonoDlFallbackSymbol symbol_func, MonoDlFallbackClose close_func, void *user_data)
{
	MonoDlFallbackHandler *handler = NULL;
	if (load_func == NULL || symbol_func == NULL)
		goto leave;

	handler = g_new (MonoDlFallbackHandler, 1);
	handler->load_func = load_func;
	handler->symbol_func = symbol_func;
	handler->close_func = close_func;
	handler->user_data = user_data;

	fallback_handlers = g_slist_prepend (fallback_handlers, handler);
	
leave:
	return handler;
}

void
mono_dl_fallback_unregister (MonoDlFallbackHandler *handler)
{
	GSList *found;

	found = g_slist_find (fallback_handlers, handler);
	if (found == NULL)
		return;

	g_slist_remove (fallback_handlers, handler);
	g_free (handler);
}

static MonoDl*
try_load (const char *lib_name, char *dir, int flags, char **err)
{
	gpointer iter;
	MonoDl *runtime_lib;
	char *path;
	iter = NULL;
	*err = NULL;
	while ((path = mono_dl_build_path (dir, lib_name, &iter))) {
		g_free (*err);
		runtime_lib = mono_dl_open (path, flags, err);
		g_free (path);
		if (runtime_lib)
			return runtime_lib;
	}
	return NULL;
}

MonoDl*
mono_dl_open_runtime_lib (const char* lib_name, int flags, char **error_msg)
{
	MonoDl *runtime_lib = NULL;
	char buf [4096];
	int binl;
	*error_msg = NULL;

	binl = mono_dl_get_executable_path (buf, sizeof (buf));

	if (binl != -1) {
		char *base;
		char *resolvedname, *name;
		char *baseparent = NULL;
		buf [binl] = 0;
		resolvedname = mono_path_resolve_symlinks (buf);
		base = g_path_get_dirname (resolvedname);
		name = g_strdup_printf ("%s/.libs", base);
		runtime_lib = try_load (lib_name, name, flags, error_msg);
		g_free (name);
		if (!runtime_lib)
			baseparent = g_path_get_dirname (base);
		if (!runtime_lib) {
			name = g_strdup_printf ("%s/lib", baseparent);
			runtime_lib = try_load (lib_name, name, flags, error_msg);
			g_free (name);
		}
#ifdef __MACH__
		if (!runtime_lib) {
			name = g_strdup_printf ("%s/Libraries", baseparent);
			runtime_lib = try_load (lib_name, name, flags, error_msg);
			g_free (name);
		}
#endif
		if (!runtime_lib) {
			name = g_strdup_printf ("%s/profiler/.libs", baseparent);
			runtime_lib = try_load (lib_name, name, flags, error_msg);
			g_free (name);
		}
		g_free (base);
		g_free (resolvedname);
		g_free (baseparent);
	}
	if (!runtime_lib)
		runtime_lib = try_load (lib_name, NULL, flags, error_msg);

	return runtime_lib;
}
