#ifndef __WINE_FTRACE__
#define __WINE_FTRACE__

#include <stdarg.h>

unsigned int __wine_dbg_ftrace( char *str, unsigned int str_size, unsigned int ctx );
int __wine_dbg_ftrace_init(void);
extern int ftrace_fd;

static inline unsigned int __wine_dbg_ftrace_printf( unsigned int ctx, const char *format, ...)
{
    char buffer[256];

    va_list args;

    va_start( args, format );
    vsnprintf( buffer, sizeof(buffer), format, args );
    va_end( args );
    return __wine_dbg_ftrace(buffer, sizeof(buffer), ctx);
}

#define FTRACE_ON (ftrace_fd == -1 ? __wine_dbg_ftrace_init() : ftrace_fd >= 0)

#define FTRACE(...)                do { if (FTRACE_ON) __wine_dbg_ftrace_printf( -1, __VA_ARGS__ ); } while (0)

#define FTRACE_BLOCK_START(...) do { \
                                    unsigned int ctx = FTRACE_ON ? __wine_dbg_ftrace_printf( 0, __VA_ARGS__ ) : 0; \
                                    do {

#define FTRACE_BLOCK_END()          } while (0); \
                                    if (FTRACE_ON) __wine_dbg_ftrace_printf( ctx, "" ); \
                                } while (0);

#endif
