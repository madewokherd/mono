#include <stdlib.h>
#include <fcntl.h>
#include <stdio.h>

#include <mono/utils/atomic.h>

#include "ftrace.h"

int ftrace_fd = -1;

int __wine_dbg_ftrace_init(void)
{
    if (ftrace_fd == -1)
    {
        const char *fn;
        int fd;

        if (!(fn = getenv( "WINE_FTRACE_FILE" )))
        {
            ftrace_fd = -2;
            return 0;
        }
        if ((fd = open( fn, O_WRONLY )) == -1)
        {
            ftrace_fd = -2;
            return 0;
        }
        if (mono_atomic_cas_i32(&ftrace_fd, fd, -1) != -1)
            close( fd );
    }

    return ftrace_fd >= 0;
}

unsigned int __wine_dbg_ftrace( char *str, unsigned int str_size, unsigned int ctx )
{
    static volatile gint32 curr_ctx;
    unsigned int str_len;
    char ctx_str[64];
    int ctx_len;

    if (ftrace_fd < 0) return ~0u;

    if (ctx == ~0u) ctx_len = 0;
    else if (ctx) ctx_len = sprintf( ctx_str, " (end_ctx=%u)", ctx );
    else
    {
        ctx = mono_atomic_inc_i32(&curr_ctx);
        ctx_len = sprintf( ctx_str, " (begin_ctx=%u)", ctx );
    }

    str_len = strlen(str);
    if (ctx_len > 0)
    {
        if (str_size < ctx_len) return ~0u;
        if (str_len + ctx_len > str_size) str_len = str_size - ctx_len;
        memcpy( &str[str_len], ctx_str, ctx_len );
        str_len += ctx_len;
    }
    write( ftrace_fd, str, str_len );
    return ctx;
}
