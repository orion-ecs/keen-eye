using KeenEyes.Lsp.Kesl;

// Configure console for binary I/O
Console.InputEncoding = System.Text.Encoding.UTF8;
Console.OutputEncoding = System.Text.Encoding.UTF8;

var server = new KeslLanguageServer(Console.OpenStandardInput(), Console.OpenStandardOutput());

await server.RunAsync();
