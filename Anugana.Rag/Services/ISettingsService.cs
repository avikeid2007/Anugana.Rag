using Anugana.Rag.Models;

namespace Anugana.Rag.Services;

public interface ISettingsService
{
    AppSettings CurrentSettings { get; }
    void SaveSettings(AppSettings settings);
}
