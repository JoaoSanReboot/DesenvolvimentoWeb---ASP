using System.Net;
using System.Net.Sockets;
using System.Text;

class ServidorHttp
{

    private TcpListener Controlador { get; set; }

    private int Porta { get; set; }

    private int QtdeRequests { get; set; }

//Metodo Construtor
    public ServidorHttp(int porta = 8080)
    {
        this.Porta = porta;

        //Objeto para escutar a porta.
        try
        {
            this.Controlador = new TcpListener(IPAddress.Parse("127.0.0.1"), this.Porta);
            this.Controlador.Start();
            Console.WriteLine($"Servidor HTTP está rodando na porta {this.Porta}.");
            Console.WriteLine($"Para acessar, digite no navegador: http://localhost:{this.Porta}.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao iniciar o servidor na porta {this.Porta}: \n{e.Message}");
        }
    }

    //Recebe a requisição
    private async Task AguardarRequests()
    {
        while (true)
        {
            Socket conexao = await this.Controlador.AcceptSocketAsync();
            this.QtdeRequests++;
        }
    }


    //Processa a requisição
    private void ProcessarRequest(Socket conexao, int numeroRequest)
    {
        Console.WriteLine($"Processando request #{numeroRequest}...\n");
        if (conexao.Connected)
        {
            byte[] bytesRequesicao = new byte[1024];
            conexao.Receive(bytesRequesicao, bytesRequesicao.Length, 0);
            string textoRequesicao = Encoding.UTF8.GetString(bytesRequesicao)
                .Replace((char)0, ' ').Trim();
        }
    }
}
