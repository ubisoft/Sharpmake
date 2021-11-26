#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>

int main( int argc, char ** argv )
{
    if ( argc != 3 )
    {
        printf( "Bad Args!\n" );
        return 1;
    }

    const char * fileToStamp = argv[ 1 ];
    const char * stampMessage = argv[ 2 ];

    FILE * f = fopen( fileToStamp, "ab+" );
    if ( f == NULL )
    {
        printf( "Can't open for append file %s!\n", fileToStamp );
        return 1;
    }

    fwrite( (char *) stampMessage, strlen( stampMessage ), 1, f );
    fclose( f );
    return 0;
}
