using TenantDoc.Core.Models;

namespace TenantDoc.Core.Interfaces;

public interface IDocumentStore
{
    void Add(Document document);
    Document? Get(Guid id);
    IEnumerable<Document> GetAll();
    void Delete(Guid id);
}
