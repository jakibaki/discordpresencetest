#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <time.h>
#include <string.h>
#include <stdio.h>

#include <unistd.h>

#include <stdbool.h>
#include <readline/readline.h>

#define MAGIC_HEADER 0xffaadd23
#define MAGIC_IMG 0xaabbdd32
#define PORT 51966

#ifndef __SWITCH__
typedef __uint8_t u8;
typedef __uint16_t u16;
typedef __uint32_t u32;
typedef __uint64_t u64;
#endif

struct pkg_header
{
    u32 magic;
    char name[256];
    u32 img_size;
};

#define IMG_BUF_SIZE 32768
struct pkg_img
{
    u32 img_magic;
    u32 index;
    u32 used_size;
    char img[IMG_BUF_SIZE];
};

int main()
{
    int sockfd;
    struct sockaddr_in servaddr;

    sockfd = socket(AF_INET, SOCK_DGRAM, 0);

    int broadcastEnable = 1;
    int ret = setsockopt(sockfd, SOL_SOCKET, SO_BROADCAST, &broadcastEnable, sizeof(broadcastEnable));

    memset(&servaddr, 0, sizeof(servaddr));

    // Filling server information
    servaddr.sin_family = AF_INET;
    servaddr.sin_port = htons(PORT);
    inet_aton("127.0.0.1", &servaddr.sin_addr);

    int n, len;

    while (true)
    {
        char *gamename = readline("Enter game name: ");
        char *filename = readline("Enter img file name: ");

        // This is where you'd obviously use your image instead of getting it from a file
        FILE *f = fopen(filename, "r");
        fseek(f, 0L, SEEK_END);
        size_t imgsize = ftell(f);
        rewind(f);

        struct pkg_header pkg = {0};
        pkg.magic = MAGIC_HEADER;
        pkg.img_size = imgsize;
        strncpy(pkg.name, gamename, 255);

        sendto(sockfd, (const char *)&pkg, sizeof(pkg),
               0, (const struct sockaddr *)&servaddr,
               sizeof(servaddr));
        u32 index = 0;
        u32 sent_size;
        /*do
        {
            struct pkg_img pkg_img;
            pkg_img.img_magic = MAGIC_IMG;
            pkg_img.index = index;
            sent_size = fread(pkg_img.img, 1, IMG_BUF_SIZE, f);
            sendto(sockfd, (const char *)&pkg_img, sizeof(pkg_img),
                   0, (const struct sockaddr *)&servaddr,
                   sizeof(servaddr));
            index++;
        } while (sent_size == IMG_BUF_SIZE);*/

        fclose(f);

        free(gamename);
        free(filename);
    }
}