using System.Text;

class PaginaProdutos : PaginaDinamica 
{
    public override byte[] Get(SortedList<string, string> parametros)
    {
        StringBuilder htmlGerado = new StringBuilder();
        foreach (var p in Produto.Listagem)
        {
            htmlGerado.Append("<tr>");

            htmlGerado.Append("</tr>");
        }
    }
}