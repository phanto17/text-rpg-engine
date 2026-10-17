using RpgEngine.CLI;
using Spectre.Console;

try
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
}
catch (IOException)
{
    // Some hosts do not allow changing the encoding; Spectre falls back to ASCII-safe output.
}

return CliHost.Run(args, AnsiConsole.Console);
