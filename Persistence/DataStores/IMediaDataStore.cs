using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Persistence.DbEntities;

namespace Persistence.DataStores
{
    public interface IMediaDataStore
    {
        Task<Result<Media>> GetMedia(string mediaName);
    }
}