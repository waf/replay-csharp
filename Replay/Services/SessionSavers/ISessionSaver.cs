using System.Collections.Generic;
using System.Threading.Tasks;

namespace Replay.Services.SessionSavers
{
    interface ISessionSaver
    {
        string SaveFormat { get; }
        Task<string> SaveAsync(string fileName, IReadOnlyCollection<LineToSave> linesToSave);
    }
}