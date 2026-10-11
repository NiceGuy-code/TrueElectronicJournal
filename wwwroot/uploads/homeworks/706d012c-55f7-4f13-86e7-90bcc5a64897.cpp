#include <iostream>

int main()
{
    std::cout << "Hello World!\n";
    char a{ 'a' };
    char b{ 'b' };
    char c{ a + b };
    std::string str{ "123" };

    char d{ str[3] };
    std::cout << c << d;
}
