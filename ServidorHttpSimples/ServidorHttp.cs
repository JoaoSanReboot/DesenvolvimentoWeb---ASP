//Bibliotecas

using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;

class ServidorHttp
{

    //Defino os encapsulamentos da classe.

    private TcpListener Controlador { get; set; }

    private int Porta { get; set; }

    private int QtdeRequests { get; set; }

    public string HtmlExemplo { get; set; }

    private SortedList<string, string> TiposMime { get; set; }
    private SortedList<string, string> DiretoriosHosts { get; set; }

    //O construtor do programa, coração do projeto.
    public ServidorHttp(int porta = 8080)
    {

        //Defino quais encapsulamentos irão ser utilizados ao executar a função da classe.

        this.Porta = porta;
        this.CriarHtmlExemplo();
        this.PopularTiposMIME();
        this.PopularDiretoriosHosts();

        //Crio um objeto que "escuta" no IP definido dentro da porta.
        try
        {
            this.Controlador = new TcpListener(IPAddress.Parse("127.0.0.1"), this.Porta);
            this.Controlador.Start();
            Console.WriteLine($"Servidor HTTP está rodando na porta {this.Porta}.");
            Console.WriteLine($"Para acessar, digite no navegador: http://localhost:{this.Porta}.");
            Task servidorHttpTask = Task.Run(() => AguardarRequests());
            servidorHttpTask.GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Erro ao iniciar o servidor na porta {this.Porta}: \n{e.Message}");
        }
    }

    //Receptor da requesição.
    private async Task AguardarRequests()
    {
        while (true)
        {
            Socket conexao = await this.Controlador.AcceptSocketAsync();
            this.QtdeRequests++;
            Task task = Task.Run(() => ProcessarRequest(conexao, this.QtdeRequests));
        }
    }


    //Processa a requesição, o cerébro do projeto.
    private void ProcessarRequest(Socket conexao, int numeroRequest)
    {
        Console.WriteLine($"Processando request #{numeroRequest}...\n");
        if (conexao.Connected) //Verifico se a conexão foi conectada.
        {
            //Caso tenha sido conectada defino a quantia em memória que será alocada no sistema.
            byte[] bytesRequesicao = new byte[1024];
            conexao.Receive(bytesRequesicao, bytesRequesicao.Length, 0);
            string textoRequesicao = Encoding.UTF8.GetString(bytesRequesicao)
                .Replace((char)0, ' ').Trim(); //Depois elimino a quantia de memória não utilizada.
            if (textoRequesicao.Length > 0)
            {
                Console.WriteLine($"\n{textoRequesicao}\n");

                string[] linhas = textoRequesicao.Split("\r\n");
                int iPrimeiroEspaco = linhas[0].IndexOf(' ');
                int iSegundoEspaco = linhas[0].LastIndexOf(' ');
                string metodoHttp = linhas[0].Substring(0, iPrimeiroEspaco);
                string recursoBuscado = linhas[0].Substring(
                    iPrimeiroEspaco + 1, iSegundoEspaco - iPrimeiroEspaco - 1);
                if (recursoBuscado == "/") recursoBuscado = "/index.html";
                string textoParametros = recursoBuscado.Contains("?") ?
                    recursoBuscado.Split("?")[1] : "";
                SortedList<string, string> parametros = ProcessarParametros(textoParametros);
                string dadosPost = textoRequesicao.Contains("\r\n\r\n") ?
                    textoRequesicao.Split("\r\n\r\n")[1] : "";
                if(!string.IsNullOrEmpty(dadosPost))
                {
                    dadosPost = HttpUtility.UrlDecode(dadosPost, Encoding.UTF8);
                    var parametrosPost = ProcessarParametros(dadosPost);
                    foreach (var pp in parametrosPost)
                        parametros.Add(pp.Key, pp.Value);
                }
                recursoBuscado = recursoBuscado.Split("?")[0];
                string versaoHttp = linhas[0].Substring(iSegundoEspaco + 1);
                iPrimeiroEspaco = linhas[1].IndexOf(' ');
                string nomeHost = linhas[1].Substring(iPrimeiroEspaco + 1);

                byte[] bytesCabecalho = null;
                byte[] bytesConteudo = null;
                FileInfo fiArquivo = new FileInfo(ObterCaminhoFisicoArquivo(nomeHost, recursoBuscado));
                if (fiArquivo.Exists)
                {
                    if (TiposMime.ContainsKey(fiArquivo.Extension.ToLower()))
                    {
                        if (fiArquivo.Extension.ToLower() == ".dhtml")
                            bytesConteudo = GerarHTMLDinamico(fiArquivo.FullName, parametros, metodoHttp);
                        else
                            bytesConteudo = File.ReadAllBytes(fiArquivo.FullName);

                        string tipoMime = TiposMime[fiArquivo.Extension.ToLower()];
                        bytesCabecalho = GerarCabecalho(versaoHttp, tipoMime,
                            "200", bytesConteudo.Length);
                    }
                    else
                    {
                        bytesConteudo = Encoding.UTF8.GetBytes(
                            "<h1> Erro 415 - Tipo de arquivo não suportado. </h1>");
                        bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8",
                        "415", bytesConteudo.Length);
                    }
                }
                else
                {
                    bytesConteudo = Encoding.UTF8.GetBytes(
                        "<h1>Erro 404 - Arquivo Não Encontrado</h1>");
                    bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8",
                    "404", bytesConteudo.Length);
                }

                int bytesEnviados = conexao.Send(bytesCabecalho, bytesCabecalho.Length, 0);
                bytesEnviados += conexao.Send(bytesConteudo, bytesConteudo.Length, 0);
                conexao.Close();
                Console.WriteLine($"\n{bytesEnviados} bytes enviados em resposta à requesição # {numeroRequest}.");
            }
        }
        Console.WriteLine($"\nRequest {numeroRequest} finalizado.");
    }

    public byte[] GerarCabecalho(string versaoHttp, string tipoMime,
    string codigoHttp, int qtdBytes = 0)
    {
        StringBuilder texto = new StringBuilder();
        texto.Append($"{versaoHttp} {codigoHttp}{Environment.NewLine}");
        texto.Append($"Server: Servidor Http Simples 1.0 {Environment.NewLine}");
        texto.Append($"Content-Type: {tipoMime}{Environment.NewLine}");
        texto.Append($"Content-Length: {qtdBytes}{Environment.NewLine}{Environment.NewLine}");
        return Encoding.UTF8.GetBytes(texto.ToString());
    }

    private void CriarHtmlExemplo()
    {
        StringBuilder html = new StringBuilder();
        html.Append("<!DOCTYPE html><html lang=\"pt-br\"><head><meta charset=\"UTF-8\">");
        html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.Append("<title>Página Estática</title><head><body>");
        html.Append("<h1>Página Estática</h1></body></html>");
        this.HtmlExemplo = html.ToString();
    }

    private void PopularTiposMIME()
    {
        this.TiposMime = new SortedList<string, string>();
        this.TiposMime.Add(".html", "text/html;charset=utf-8");
        this.TiposMime.Add(".htm", "text/html;charset=utf-8");
        this.TiposMime.Add(".css", "text/css");
        this.TiposMime.Add(".js", "text/javascript");
        this.TiposMime.Add(".png", "image/png");
        this.TiposMime.Add(".jpeg", "image/jpeg");
        this.TiposMime.Add(".gif", "image/gif");
        this.TiposMime.Add(".svg", "image/svg+xml");
        this.TiposMime.Add(".webp", "image/webp");
        this.TiposMime.Add(".ico", "image/ico");
        this.TiposMime.Add("woff", "font/woff");
        this.TiposMime.Add("woff2", "font/woff2");
        this.TiposMime.Add(".dhtml", "text/html;charset=utf-8");
    }

    private void PopularDiretoriosHosts()
    {

        this.DiretoriosHosts = new SortedList<string, string>();
        this.DiretoriosHosts.Add("localhost", "C:\\Users\\santa\\OneDrive\\Documentos\\DesenvolvimentoWeb---ASP\\ServidorHttpSimples\\www\\localhost");
        this.DiretoriosHosts.Add("ronaldinho.com", "C:\\Users\\santa\\OneDrive\\Documentos\\DesenvolvimentoWeb---ASP\\ServidorHttpSimples\\www\\ronaldinho.com");

    }

    public string ObterCaminhoFisicoArquivo(string host, string arquivo)
    {
        string diretorio = this.DiretoriosHosts[host.Split(":")[0]];
        string caminhoArquivo = diretorio + arquivo.Replace("/", "\\");
        return caminhoArquivo;
    }
    
    public byte[] GerarHTMLDinamico(string caminhoArquivo, SortedList<string, string> parametros, string metodoHttp)
    {
       FileInfo fiArquivo = new FileInfo(caminhoArquivo);
       string nomeClassePagina = "Pagina" + fiArquivo.Name.Replace(fiArquivo.Extension, "");
       Type tipoPaginaDinamica = Type.GetType(nomeClassePagina, true, true);
       PaginaDinamica pd = Activator.CreateInstance(tipoPaginaDinamica) as PaginaDinamica;
       pd.HtmlModelo = File.ReadAllText(caminhoArquivo);
       switch (metodoHttp.ToLower())
       {
            case "get":
                return pd.Get(parametros);
            case "post":
                return pd.Post(parametros);
            default:
                return new byte[0];
       }
    }

    private SortedList<string, string> ProcessarParametros(string textoParametros)
    {
        SortedList<string, string> parametros = new SortedList<string, string>();
        if (!string.IsNullOrEmpty(textoParametros.Trim()))
        {
            string[] paresChaveValor = textoParametros.Split("&");
            foreach (var par in paresChaveValor)
            {
                parametros.Add(par.Split("=")[0].ToLower(), par.Split("=")[1]);
            }
        }
        return parametros;
    }
}


