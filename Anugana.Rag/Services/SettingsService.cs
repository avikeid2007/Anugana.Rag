using System;
using System.IO;
using Anugana.Rag.Models;
using SQLite;

namespace Anugana.Rag.Services;

public class SettingsService : ISettingsService
{
    private readonly string _dbPath;
    private AppSettings _settings;

    public AppSettings CurrentSettings => _settings;

    public SettingsService()
    {
        var localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!Directory.Exists(localFolder))
        {
            Directory.CreateDirectory(localFolder);
        }

        _dbPath = Path.Combine(localFolder, "anugana_settings.db");

        using var db = new SQLiteConnection(_dbPath);
        db.CreateTable<AppSettings>();

        var existing = db.Table<AppSettings>().FirstOrDefault(s => s.Id == 1);
        if (existing != null)
        {
            _settings = existing;
        }
        else
        {
            _settings = new AppSettings { Id = 1 };
            db.Insert(_settings);
        }

        Presentation.ThemeHelper.ApplyTheme(_settings.Theme);
    }

    public void SaveSettings(AppSettings settings)
    {
        settings.Id = 1;
        _settings = settings;

        using var db = new SQLiteConnection(_dbPath);
        db.CreateTable<AppSettings>();
        db.InsertOrReplace(_settings);

        Presentation.ThemeHelper.ApplyTheme(_settings.Theme);
    }
}
