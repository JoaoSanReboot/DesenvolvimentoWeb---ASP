
using System.Text;

class PaginaCadastroProduto : PaginaDinamica
{
public override byte[] Post(SortedList<string, string> parametros)
{
    try
    {
        Produto p = new Produto();
        p.Codigo = parametros.ContainsKey("codigo") ?
            Convert.ToInt32(parametros["codigo"]) : 0;
        p.Nome = parametros.ContainsKey("nome") ?
            parametros["nome"] : "";

        if (p.Codigo > 0)
            Produto.Listagem.Add(p);

        string html = "<script>window.location.replace(\"produtos.dhtml\")</script>";

            Console.WriteLine("Resposta enviada: " + html);
            return Encoding.UTF8.GetBytes(html);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no servidor: {ex.Message}");
            return Encoding.UTF8.GetBytes("<h1>Erro no servidor</h1>");
        }
    }   


}