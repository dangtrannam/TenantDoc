using System.Collections.Concurrent;
using TenantDoc.Core.Interfaces;
using TenantDoc.Core.Models;

namespace TenantDoc.Infrastructure.Storage;

public class InMemoryDocumentStore : IDocumentStore
{
    private readonly ConcurrentDictionary<Guid, Document> _documents = new();

    public void Add(Document document)
    {
        _documents[document.Id] = document;
    }

    public Document? Get(Guid id)
    {
        _documents.TryGetValue(id, out var document);
        return document;
    }

    public IEnumerable<Document> GetAll() => _documents.Values;
}
