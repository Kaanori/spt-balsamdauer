// ---------------------------------------------------------------
//  Balsam-Dauer
//
//  Setzt beim Serverstart die Wirkungsdauer des Goldstern-Balsams
//  (golden_star_balm) auf einen einstellbaren Wert. Standard: 15 min.
//
//  Ab Werk haelt der Balsam
//      Schmerz (Pain)          350 s  = 5:50 min
//      Strahlung (RadExposure) 400 s  = 6:40 min
//  Beide werden auf die eingestellte Dauer gehoben.
//
//  Warum ein eigener Mod:
//  SVM kennt keine Effektdauern einzelner Gegenstaende - im ganzen
//  Vorgabensatz steht dazu nichts. Und die Datenbank direkt anfassen
//  geht seit SPT 4.1 nicht mehr: sie ist pruefsummengesichert, eine
//  geaenderte items.json bricht den Serverstart ab. Mods duerfen
//  dieselbe Tabelle zur Laufzeit aendern - dafuer sind sie da.
//
//  Absichtlich NICHT angefasst: die drei StimulatorBuffs des Balsams
//  (BuffsGoldenStarBalm, je 5 s). Das sind +1 Energie und +1 Fluessig-
//  keit PRO SEKUNDE - auf 15 min gezogen waeren das +900 Energie und
//  +900 Fluessigkeit aus einem Balsam. Die 5 s sind hier kein Tippfehler
//  von BSG, sondern die Art, wie die Aufnahme verrechnet wird.
//
//  Die Dauer steht in balsam.json neben dieser DLL, damit man sie ohne
//  Neubau aendern kann.
// ---------------------------------------------------------------
using System.Text.Json;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;

// „Path“ gibt es auch in den EFT-Modellen (Wegpunkte) - Alias setzen.
using Pfad = System.IO.Path;

namespace BalsamDauer;

public record ModMetadata : IModMetadata
{
	public string ModGuid { get; init; } = "com.kaan.balsamdauer";
	public string Name { get; init; } = "Balsam-Dauer";
	public string Author { get; init; } = "Kaan";
	public List<string>? Contributors { get; init; } = new();
	public SemanticVersioning.Version Version { get; init; } = new(1, 0, 0);
	public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1");
	public bool HasPrepatcher { get; init; } = false;
	public List<string>? Incompatibilities { get; init; } = new();
	public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new();
	public string? Url { get; init; } = "";
	public string License { get; init; } = "MIT";
}

[Injectable(InjectionType.Singleton)]
public class BalsamVerlaengerer(
	ISptLogger<BalsamVerlaengerer> logger,
	TemplateTable vorlagen) : IOnLoad
{
	// Goldstern-Balsam ("Zvezdochka"), Vorlagen-ID aus templates/items.json.
	private const string BalsamId = "5751a89d24597722aa0e8db0";

	public Task OnLoadAsync(CancellationToken abbruch)
	{
		try
		{
			var (minuten, effekte) = EinstellungenLesen();
			if (minuten <= 0)
			{
				logger.Info("[Balsam-Dauer] minuten <= 0 - nichts zu tun.");
				return Task.CompletedTask;
			}

			var sekunden = minuten * 60.0;

			if (!vorlagen.Items.TryGetValue(BalsamId, out var balsam))
			{
				logger.Warning($"[Balsam-Dauer] Vorlage {BalsamId} nicht gefunden - "
				               + "hat sich die ID geaendert?");
				return Task.CompletedTask;
			}

			var schaden = balsam.Properties?.EffectsDamage;
			if (schaden is null || schaden.Count == 0)
			{
				logger.Warning("[Balsam-Dauer] Der Balsam hat keine effects_damage - "
				               + "nichts zu aendern.");
				return Task.CompletedTask;
			}

			var geaendert = new List<string>();

			foreach (var name in effekte)
			{
				if (!Enum.TryParse<DamageEffectType>(name, ignoreCase: true, out var art))
				{
					logger.Warning($"[Balsam-Dauer] Unbekannter Effekt \"{name}\" in "
					               + "balsam.json - uebersprungen.");
					continue;
				}

				if (!schaden.TryGetValue(art, out var eigenschaften))
				{
					logger.Warning($"[Balsam-Dauer] Der Balsam hat keinen Effekt {art} - "
					               + "uebersprungen.");
					continue;
				}

				var vorher = eigenschaften.Duration ?? 0;
				eigenschaften.Duration = sekunden;
				geaendert.Add($"{art} {vorher:0} s -> {sekunden:0} s");
			}

			if (geaendert.Count == 0)
			{
				logger.Warning("[Balsam-Dauer] Kein einziger Effekt geaendert.");
				return Task.CompletedTask;
			}

			logger.Success($"[Balsam-Dauer] Goldstern-Balsam auf {minuten} min: "
			               + string.Join(", ", geaendert) + ".");
		}
		catch (Exception ex)
		{
			logger.Error("[Balsam-Dauer] " + ex.Message);
		}

		return Task.CompletedTask;
	}

	private (double Minuten, List<string> Effekte) EinstellungenLesen()
	{
		var standard = (15.0, new List<string> { "Pain", "RadExposure" });

		var ordner = Pfad.GetDirectoryName(typeof(BalsamVerlaengerer).Assembly.Location);
		var datei = Pfad.Combine(ordner ?? ".", "balsam.json");
		if (!File.Exists(datei))
		{
			logger.Info("[Balsam-Dauer] Keine balsam.json - 15 min fuer Pain und RadExposure.");
			return standard;
		}

		try
		{
			using var papier = JsonDocument.Parse(File.ReadAllText(datei));
			var wurzel = papier.RootElement;

			var minuten = wurzel.TryGetProperty("minuten", out var m) && m.TryGetDouble(out var wert)
				? wert
				: 15.0;

			var effekte = new List<string>();
			if (wurzel.TryGetProperty("effekte", out var e)
			    && e.ValueKind == JsonValueKind.Array)
			{
				foreach (var eintrag in e.EnumerateArray())
				{
					var text = eintrag.GetString();
					if (!string.IsNullOrWhiteSpace(text)) effekte.Add(text);
				}
			}

			if (effekte.Count == 0) effekte = standard.Item2;

			return (minuten, effekte);
		}
		catch (Exception ex)
		{
			logger.Error("[Balsam-Dauer] balsam.json ließ sich nicht lesen ("
			             + ex.Message + ") - 15 min fuer Pain und RadExposure.");
			return standard;
		}
	}
}
