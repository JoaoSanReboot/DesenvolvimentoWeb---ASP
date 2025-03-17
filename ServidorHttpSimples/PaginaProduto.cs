using System.Text;

class PaginaProdutos : PaginaDinamica 
{
    public override byte[] Get(SortedList<string, string> parametros)
    {
        StringBuilder htmlGerado = new StringBuilder();
        foreach (var p in Produto.Listagem)
        {
            htmlGerado.Append("<tr>");
            htmlGerado.Append($"<td>{p.Codigo:D4}</td>");
            htmlGerado.Append($"<td>{p.Nome}</td>");
            htmlGerado.Append("</tr>");
        }

        string textoHtmlGerado = this.HtmlModelo.Replace(
            "{{HtmlGerado}}", htmlGerado.ToString());
        return Encoding.UTF8.GetBytes(textoHtmlGerado);
    }
}