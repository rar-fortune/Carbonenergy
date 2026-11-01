using System;
using System.Collections.Generic;
using System.IO;
using System.Media;

namespace CarbonEnergy
{
    // ==========================================
    // PROCEDURAL AUDIO SYNTHESIZER
    // ==========================================
    public static class SoundEngine
    {
        public static bool SoundEnabled = true;
        public static bool MusicEnabled = true;
        public static float Volume = 0.8f;

        public static void PlayClick()
        {
            if (!SoundEnabled) return;
            PlaySynthTone(880, 0.04, 0.25, Waveform.Sine);
        }

        public static void PlayCash()
        {
            if (!SoundEnabled) return;
            PlaySynthArpeggio(new int[] { 587, 880, 1174 }, 0.06, 0.2);
        }

        public static void PlayUpgrade()
        {
            if (!SoundEnabled) return;
            PlaySynthArpeggio(new int[] { 523, 659, 784, 1046 }, 0.08, 0.3);
        }

        public static void PlayEcoAction()
        {
            if (!SoundEnabled) return;
            PlaySynthTone(1200, 0.12, 0.2, Waveform.Triangle);
        }

        public static void PlayAlarm()
        {
            if (!SoundEnabled) return;
            PlaySynthArpeggio(new int[] { 440, 660, 440, 660 }, 0.09, 0.35);
        }

        public static void PlayDilemma()
        {
            if (!SoundEnabled) return;
            PlaySynthTone(440, 0.18, 0.25, Waveform.Sine);
        }

        public static void PlayVictory()
        {
            if (!SoundEnabled) return;
            PlaySynthArpeggio(new int[] { 523, 659, 784, 1046, 1318 }, 0.14, 0.35);
        }

        public static void PlayAchievement()
        {
            if (!SoundEnabled) return;
            PlaySynthArpeggio(new int[] { 523, 659, 784, 1046, 1318, 1568 }, 0.07, 0.35);
        }

        public static void PlayAmbientChord(int phraseIndex)
        {
            if (!SoundEnabled || !MusicEnabled) return;
            int[][] chords = new int[][] {
                new int[] { 330, 392, 494, 659 },
                new int[] { 262, 330, 392, 523 },
                new int[] { 294, 370, 440, 587 },
                new int[] { 392, 494, 587, 784 }
            };
            int[] chord = chords[phraseIndex % chords.Length];
            PlaySynthArpeggio(chord, 0.28, 0.10);
        }

        public static void PlayDefeat()
        {
            if (!SoundEnabled) return;
            PlaySynthArpeggio(new int[] { 440, 415, 392, 349 }, 0.2, 0.35);
        }

        private enum Waveform { Sine, Triangle, Square }

        private static void PlaySynthTone(double frequency, double durationSeconds, double volume, Waveform wave)
        {
            try
            {
                int sampleRate = 22050;
                int totalSamples = (int)(sampleRate * durationSeconds);
                byte[] rawData = new byte[totalSamples * 2];

                for (int i = 0; i < totalSamples; i++)
                {
                    double t = (double)i / sampleRate;
                    double angle = 2.0 * Math.PI * frequency * t;
                    double sample = 0;

                    if (wave == Waveform.Sine)
                    {
                        sample = Math.Sin(angle);
                    }
                    else if (wave == Waveform.Triangle)
                    {
                        sample = (2.0 / Math.PI) * Math.Asin(Math.Sin(angle));
                    }
                    else
                    {
                        sample = Math.Sin(angle) >= 0 ? 0.7 : -0.7;
                    }

                    double envelope = 1.0 - ((double)i / totalSamples);
                    short val = (short)(sample * volume * Volume * envelope * 32767);

                    rawData[i * 2] = (byte)(val & 0xff);
                    rawData[i * 2 + 1] = (byte)((val >> 8) & 0xff);
                }

                byte[] wav = CreateWavHeader(rawData, sampleRate, 1, 16);
                using (MemoryStream ms = new MemoryStream(wav))
                {
                    SoundPlayer player = new SoundPlayer(ms);
                    player.Play();
                }
            }
            catch
            {
                try { SystemSounds.Beep.Play(); } catch { }
            }
        }

        private static void PlaySynthArpeggio(int[] freqs, double noteDuration, double volume)
        {
            try
            {
                int sampleRate = 22050;
                int samplesPerNote = (int)(sampleRate * noteDuration);
                int totalSamples = samplesPerNote * freqs.Length;
                byte[] rawData = new byte[totalSamples * 2];

                for (int n = 0; n < freqs.Length; n++)
                {
                    double freq = freqs[n];
                    int startSample = n * samplesPerNote;

                    for (int i = 0; i < samplesPerNote; i++)
                    {
                        double t = (double)i / sampleRate;
                        double angle = 2.0 * Math.PI * freq * t;
                        double sample = Math.Sin(angle);

                        double envelope = 1.0 - ((double)i / samplesPerNote);
                        short val = (short)(sample * volume * Volume * envelope * 32767);

                        int byteIndex = (startSample + i) * 2;
                        rawData[byteIndex] = (byte)(val & 0xff);
                        rawData[byteIndex + 1] = (byte)((val >> 8) & 0xff);
                    }
                }

                byte[] wav = CreateWavHeader(rawData, sampleRate, 1, 16);
                using (MemoryStream ms = new MemoryStream(wav))
                {
                    SoundPlayer player = new SoundPlayer(ms);
                    player.Play();
                }
            }
            catch
            {
                try { SystemSounds.Asterisk.Play(); } catch { }
            }
        }

        private static byte[] CreateWavHeader(byte[] pcmData, int sampleRate, short channels, short bitsPerSample)
        {
            byte[] wav = new byte[44 + pcmData.Length];
            int byteRate = sampleRate * channels * (bitsPerSample / 8);
            short blockAlign = (short)(channels * (bitsPerSample / 8));
            int dataChunkSize = pcmData.Length;
            int totalChunkSize = 36 + dataChunkSize;

            wav[0] = (byte)'R'; wav[1] = (byte)'I'; wav[2] = (byte)'F'; wav[3] = (byte)'F';
            Buffer.BlockCopy(BitConverter.GetBytes(totalChunkSize), 0, wav, 4, 4);
            wav[8] = (byte)'W'; wav[9] = (byte)'A'; wav[10] = (byte)'V'; wav[11] = (byte)'E';
            wav[12] = (byte)'f'; wav[13] = (byte)'m'; wav[14] = (byte)'t'; wav[15] = (byte)' ';
            Buffer.BlockCopy(BitConverter.GetBytes(16), 0, wav, 16, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((short)1), 0, wav, 20, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(channels), 0, wav, 22, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(sampleRate), 0, wav, 24, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(byteRate), 0, wav, 28, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(blockAlign), 0, wav, 32, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(bitsPerSample), 0, wav, 34, 2);
            wav[36] = (byte)'d'; wav[37] = (byte)'a'; wav[38] = (byte)'t'; wav[39] = (byte)'a';
            Buffer.BlockCopy(BitConverter.GetBytes(dataChunkSize), 0, wav, 40, 4);

            Buffer.BlockCopy(pcmData, 0, wav, 44, pcmData.Length);
            return wav;
        }
    }

    // ==========================================
    // UPGRADE DEFINITION
    // ==========================================
    public class UpgradeItem
    {
        public string Id;
        public string Name;
        public string Category;
        public string Description;
        public int Level;
        public int MaxLevel;
        public double BaseCost;
        public double CostMultiplier;
        public double MonthlyMaintenance;
        public double EmissionReductionPercent;
        public double CleanEnergyOutputMWh;
        public double ForestAbsorptionTons;
        public double PublicApprovalBonus;
        public string IconSymbol;

        public double GetCurrentCost()
        {
            return BaseCost * Math.Pow(CostMultiplier, Level);
        }

        public bool IsMaxed()
        {
            return Level >= MaxLevel;
        }
    }

    // ==========================================
    // MANAGERIAL DILEMMA / EVENT (MULTIPLE CHOICES)
    // ==========================================
    public class DilemmaChoice
    {
        public string Key; // "A", "B", "C", "D"
        public string Title;
        public string Text;
        public string ConsequenceSummary;
        public double Cost;
        public double Revenue;
        public double EmissionChangePercent;
        public double ClimateThreatChange;
        public double PublicApprovalChange;
        public double CleanEnergyBonus;
        public string ResultNarrative;
    }

    public class DilemmaEvent
    {
        public string Id;
        public string Title;
        public string CharacterName;
        public string CharacterRole;
        public string Description;
        public List<DilemmaChoice> Choices;
        public bool Triggered;
        public string SelectedChoiceKey;
        public int TriggeredMonth;

        public DilemmaEvent()
        {
            Choices = new List<DilemmaChoice>();
            Triggered = false;
            SelectedChoiceKey = "";
            TriggeredMonth = 0;
        }
    }

    // ==========================================
    // CLIMATE AWARENESS FACT
    // ==========================================
    public class ClimateFact
    {
        public string Title;
        public string Body;
        public string Source;
        public string ActionCall;
    }

    // ==========================================
    // ACHIEVEMENTS / MÜDÜRLÜK MADALYALARI
    // ==========================================
    public class AchievementItem
    {
        public string Id;
        public string Title;
        public string Description;
        public string Icon;
        public bool Unlocked;
        public int UnlockedMonth;
        public double CurrentProgress;
        public double TargetProgress;
        public string Unit;
        public double CashBonus;

        public int GetProgressPercent()
        {
            if (Unlocked) return 100;
            if (TargetProgress <= 0) return 0;
            return (int)Math.Min(100, Math.Max(0, (CurrentProgress / TargetProgress) * 100));
        }
    }

    // ==========================================
    // CORE GAME MODEL (HARDENED ECONOMY & MANUAL CONTRACTS)
    // ==========================================
    public class GameModel
    {
        // 60-Month Mandate (5 Years)
        public int CurrentMonth = 1;
        public int MaxMonths = 60;
        public int StartYear = 2026;
        public bool IsPaused = false;
        public int GameSpeed = 1;
        public bool GameOver = false;
        public string GameOverReason = "";
        public bool Victory = false;
        public string VictoryTier = "";

        // Player & Difficulty
        public string PlayerName = "Ali Kaya";
        public float SoundVolume = 0.8f;
        public bool MusicEnabled = true;
        public int Difficulty = 1; // 0=Kolay, 1=Normal, 2=Zor

        // Logging & History
        public List<string> EventLog = new List<string>();
        public List<DilemmaEvent> PastResolvedDilemmas = new List<DilemmaEvent>();
        public string LastEventMessage = "";
        public bool DilemmaPendingPopup = false;

        // Statistics
        public int TotalEcoActionsCount = 0;
        public int TotalCoalMinedByClick = 0;
        public int TotalTreesPlantedByAction = 0;
        public double PeakRenewablePercent = 0.0;
        public double LowestNetCarbonRecord = 9999.0;

        // HARDENED ECONOMICS (₺)
        // Starting budget is tighter
        public double Budget = 65000.0;
        public double MonthlyRevenue = 0.0;
        public double MonthlyExpenses = 0.0;
        public double MonthlyCarbonTax = 0.0;
        public double CarbonTaxRatePerTon = 20.0; // ₺20 per ton of net CO2!
        public double BaseOperationalExpense = 11500.0; // Base worker & plant wages
        public double TotalProfitsMade = 0.0;

        // RAW COAL STOCK & LOGISTICS (Manual element)
        public double RawCoalStockTons = 160.0; // Coal in depot
        public double WorkerStamina = 90.0; // 0 to 100%

        // SPOT MARKET & ENERGY CONTRACTS
        public double SpotEnergyPricePerMWh = 125.0;
        public bool ContractMunicipalityActive = true; // 450 MWh, ₺50,000
        public bool ContractIndustryActive = true; // 300 MWh, ₺38,000
        public double ContractPenaltyIncurred = 0.0;

        // Energy Production (MWh)
        public double EnergyDemand = 800.0;
        public double BaseEnergyDemand = 800.0;
        public double CoalProductionSlider = 1.0;
        public double CoalCapacityMax = 950.0;
        public double CoalEnergyOutput = 750.0;
        public double RenewableEnergyOutput = 0.0;
        public double TotalEnergyProduced = 750.0;

        // Carbon & Climate Stats
        public double BaseCoalEmissionFactor = 1.18;
        public double GrossCarbonEmissionRate = 0.0;
        public double FilterEfficiency = 0.0;
        public double CcsEfficiency = 0.0;
        public double NetCarbonEmissions = 0.0;
        public double TotalEmittedCarbon = 0.0;
        public double ForestAbsorptionRate = 0.0;
        public double GlobalWarmingTemp = 1.15;
        public double ClimateThreatPercent = 22.0;

        // Social & Operational Endpoints
        public double PublicApproval = 68.0;
        public double MineSafety = 85.0;
        public double TotalEcoScrubbedTons = 0.0;

        // Upgrades, Dilemmas, Facts, Achievements
        public List<UpgradeItem> Upgrades = new List<UpgradeItem>();
        public List<DilemmaEvent> Dilemmas = new List<DilemmaEvent>();
        public DilemmaEvent CurrentActiveDilemma = null;
        public List<ClimateFact> ClimateFacts = new List<ClimateFact>();
        public int CurrentFactIndex = 0;

        public List<AchievementItem> Achievements = new List<AchievementItem>();
        public AchievementItem LastUnlockedAchievement = null;
        public int ConsecutiveContractMonths = 0;

        // History tracker for charts
        public List<double> CarbonHistory = new List<double>();
        public List<double> TempHistory = new List<double>();
        public List<double> RenewablePercentHistory = new List<double>();
        public List<double> CarbonTaxHistory = new List<double>();

        public GameModel()
        {
            InitializeUpgrades();
            InitializeDilemmas();
            InitializeClimateFacts();
            InitializeAchievements();
            RecalculateStats();
            CheckAchievements();
        }

        public string GetSeasonName()
        {
            int m = ((CurrentMonth - 1) % 12) + 1;
            if (m == 12 || m == 1 || m == 2) return "❄️ Kış (Kar & Isınma Yükü)";
            if (m >= 3 && m <= 5) return "🌸 İlkbahar (Bahar Rüzgarı)";
            if (m >= 6 && m <= 8) return "☀️ Yaz (Güneş & Soğutma Yükü)";
            return "🍂 Sonbahar (Fırtına & Rüzgar)";
        }

        public void InitializeAchievements()
        {
            Achievements.Clear();
            Achievements.Add(new AchievementItem { Id = "ach_mine", Title = "Kara Elmas Kazısı", Description = "Depoya madenden toplam en az 500 ton kömür çıkart.", Icon = "⛏️", TargetProgress = 500, Unit = "Ton", CashBonus = 3500 });
            Achievements.Add(new AchievementItem { Id = "ach_trees", Title = "Yeşil Vadi Muhafızı", Description = "Maden etrafına toplam 2.500 fidan dikerek yutak oluştur.", Icon = "🌲", TargetProgress = 2500, Unit = "Fidan", CashBonus = 5000 });
            Achievements.Add(new AchievementItem { Id = "ach_zero", Title = "Mavi Gökyüzü Zaferi", Description = "Net karbon salımını sıfıra veya negatif değere indir.", Icon = "💨", TargetProgress = 1, Unit = "Kez", CashBonus = 8000 });
            Achievements.Add(new AchievementItem { Id = "ach_wind", Title = "Rüzgarın Efendisi", Description = "Aylık 250 MWh rüzgar enerjisi kapasitesine ulaş.", Icon = "🌪️", TargetProgress = 250, Unit = "MWh", CashBonus = 6000 });
            Achievements.Add(new AchievementItem { Id = "ach_solar", Title = "Güneş Çağı Mimarı", Description = "Aylık 200 MWh güneş enerjisi kapasitesine ulaş.", Icon = "☀️", TargetProgress = 200, Unit = "MWh", CashBonus = 6000 });
            Achievements.Add(new AchievementItem { Id = "ach_contracts", Title = "Sözleşme Şampiyonu", Description = "Şehir ve Sanayi sözleşmelerini aralıksız 8 ay eksiksiz teslim et.", Icon = "🤝", TargetProgress = 8, Unit = "Ay", CashBonus = 7500 });
            Achievements.Add(new AchievementItem { Id = "ach_rich", Title = "Enerji Baronu", Description = "Şirket kasasında ₺150.000 nakit rezervi biriktir.", Icon = "💰", TargetProgress = 150000, Unit = "₺", CashBonus = 10000 });
            Achievements.Add(new AchievementItem { Id = "ach_worker", Title = "Can Güvenliği Şampiyonu", Description = "İşçi güvenliğini ve moralini %90'ın üzerine çıkar.", Icon = "🩺", TargetProgress = 90, Unit = "%", CashBonus = 4000 });
            Achievements.Add(new AchievementItem { Id = "ach_crisis", Title = "Kriz Masası Lideri", Description = "4 kritik çevre ve sanayi krizini başarıyla çözüme kavuştur.", Icon = "⚖️", TargetProgress = 4, Unit = "Kriz", CashBonus = 5000 });
            Achievements.Add(new AchievementItem { Id = "ach_green_master", Title = "Yeşil Dönüşüm Kralı", Description = "Toplam elektrik üretiminde temiz enerji payını %75'in üzerine çıkar.", Icon = "👑", TargetProgress = 75, Unit = "%", CashBonus = 12000 });
        }

        public UpgradeItem FindUpgrade(string id)
        {
            for (int i = 0; i < Upgrades.Count; i++)
            {
                if (Upgrades[i].Id == id) return Upgrades[i];
            }
            return null;
        }

        public void CheckAchievements()
        {
            if (Achievements == null || Achievements.Count == 0) return;

            UpdateAchievement("ach_mine", TotalCoalMinedByClick);
            UpdateAchievement("ach_trees", TotalTreesPlantedByAction);

            if (NetCarbonEmissions <= 0) UpdateAchievement("ach_zero", 1);

            UpgradeItem windUp = FindUpgrade("wind");
            double windPower = (windUp != null ? windUp.Level * 175.0 : 0);
            UpdateAchievement("ach_wind", windPower);

            UpgradeItem solarUp = FindUpgrade("solar");
            double solarPower = (solarUp != null ? solarUp.Level * 140.0 : 0);
            UpdateAchievement("ach_solar", solarPower);

            if (ContractPenaltyIncurred == 0 && ContractMunicipalityActive && ContractIndustryActive)
            {
                ConsecutiveContractMonths++;
            }
            else
            {
                ConsecutiveContractMonths = 0;
            }
            UpdateAchievement("ach_contracts", ConsecutiveContractMonths);

            UpdateAchievement("ach_rich", Budget);
            UpdateAchievement("ach_worker", MineSafety);
            UpdateAchievement("ach_crisis", PastResolvedDilemmas.Count);

            double renewShare = TotalEnergyProduced > 0 ? (RenewableEnergyOutput / TotalEnergyProduced) * 100.0 : 0;
            UpdateAchievement("ach_green_master", renewShare);
        }

        private void UpdateAchievement(string id, double currentVal)
        {
            for (int i = 0; i < Achievements.Count; i++)
            {
                AchievementItem ach = Achievements[i];
                if (ach.Id == id)
                {
                    ach.CurrentProgress = Math.Max(ach.CurrentProgress, currentVal);
                    if (!ach.Unlocked && ach.CurrentProgress >= ach.TargetProgress)
                    {
                        ach.Unlocked = true;
                        ach.UnlockedMonth = CurrentMonth;
                        Budget += ach.CashBonus;
                        LastUnlockedAchievement = ach;
                        SoundEngine.PlayAchievement();
                    }
                    break;
                }
            }
        }

        public int GetUnlockedAchievementsCount()
        {
            int c = 0;
            for (int i = 0; i < Achievements.Count; i++)
            {
                if (Achievements[i].Unlocked) c++;
            }
            return c;
        }

        private void InitializeUpgrades()
        {
            Upgrades.Add(new UpgradeItem
            {
                Id = "filter",
                Name = "Elektrostatik Kuru Baca Filtresi",
                Category = "Filtreleme",
                Description = "Bacalardan çıkan kurum ve partikülleri elektrostatik yükle tutar.",
                Level = 0,
                MaxLevel = 3,
                BaseCost = 25000,
                CostMultiplier = 1.85,
                MonthlyMaintenance = 1400,
                EmissionReductionPercent = 0.20,
                CleanEnergyOutputMWh = 0,
                ForestAbsorptionTons = 0,
                PublicApprovalBonus = 5.0,
                IconSymbol = "🏭"
            });

            Upgrades.Add(new UpgradeItem
            {
                Id = "wind",
                Name = "Maden Sırtı Rüzgar Çiftliği",
                Category = "Yenilenebilir",
                Description = "Dağ sırtına yüksek kapasiteli rüzgar türbinleri kurar.",
                Level = 0,
                MaxLevel = 3,
                BaseCost = 38000,
                CostMultiplier = 1.9,
                MonthlyMaintenance = 2000,
                EmissionReductionPercent = 0,
                CleanEnergyOutputMWh = 175,
                ForestAbsorptionTons = 0,
                PublicApprovalBonus = 9.0,
                IconSymbol = "💨"
            });

            Upgrades.Add(new UpgradeItem
            {
                Id = "solar",
                Name = "Güneş Paneli Tarlası & Çatı GES",
                Category = "Yenilenebilir",
                Description = "Tesis arazisi ve çatıları yüksek verimli fotovoltaik panellerle donatır.",
                Level = 0,
                MaxLevel = 3,
                BaseCost = 32000,
                CostMultiplier = 1.85,
                MonthlyMaintenance = 1300,
                EmissionReductionPercent = 0,
                CleanEnergyOutputMWh = 140,
                ForestAbsorptionTons = 0,
                PublicApprovalBonus = 8.0,
                IconSymbol = "☀️"
            });

            Upgrades.Add(new UpgradeItem
            {
                Id = "methane",
                Name = "Metan Yakalama & Kojenerasyon",
                Category = "İleri Teknoloji",
                Description = "Kömür damarlarından sızan patlayıcı metanı çekip elektriğe dönüştürür.",
                Level = 0,
                MaxLevel = 2,
                BaseCost = 34000,
                CostMultiplier = 2.0,
                MonthlyMaintenance = 1600,
                EmissionReductionPercent = 0.12,
                CleanEnergyOutputMWh = 80,
                ForestAbsorptionTons = 0,
                PublicApprovalBonus = 6.0,
                IconSymbol = "⚡"
            });

            Upgrades.Add(new UpgradeItem
            {
                Id = "forest",
                Name = "Yeşil Kuşak Ormanlaştırma",
                Category = "Doğa & Karbon Yutağı",
                Description = "Maden etrafına binlerce fidan dikerek doğal karbon yutağı oluşturur.",
                Level = 0,
                MaxLevel = 3,
                BaseCost = 18000,
                CostMultiplier = 1.75,
                MonthlyMaintenance = 900,
                EmissionReductionPercent = 0,
                CleanEnergyOutputMWh = 0,
                ForestAbsorptionTons = 125,
                PublicApprovalBonus = 12.0,
                IconSymbol = "🌲"
            });

            Upgrades.Add(new UpgradeItem
            {
                Id = "ccs",
                Name = "Karbon Yakalama & Depolama (CCS)",
                Category = "İleri Teknoloji",
                Description = "Saf CO2'yi derin tuzlu akiferlere veya bazalt kayalara kalıcı hapseder.",
                Level = 0,
                MaxLevel = 2,
                BaseCost = 72000,
                CostMultiplier = 2.2,
                MonthlyMaintenance = 3800,
                EmissionReductionPercent = 0.35,
                CleanEnergyOutputMWh = 0,
                ForestAbsorptionTons = 0,
                PublicApprovalBonus = 12.0,
                IconSymbol = "🔬"
            });

            Upgrades.Add(new UpgradeItem
            {
                Id = "hydro",
                Name = "Dere Yatağı Mikro Hidroelektrik",
                Category = "Yenilenebilir",
                Description = "Vadiden geçen akarsuya ekolojik su türbinleri kurarak kesintisiz güç üretir.",
                Level = 0,
                MaxLevel = 3,
                BaseCost = 35000,
                CostMultiplier = 1.85,
                MonthlyMaintenance = 1400,
                EmissionReductionPercent = 0,
                CleanEnergyOutputMWh = 150,
                ForestAbsorptionTons = 0,
                PublicApprovalBonus = 9.0,
                IconSymbol = "💧"
            });

            Upgrades.Add(new UpgradeItem
            {
                Id = "geothermal",
                Name = "Jeotermal Derin Sondaj Santrali",
                Category = "Yenilenebilir",
                Description = "Galerilerin tabanındaki sıcak kayalardan kesintisiz temiz baz yük enerjisi üretir.",
                Level = 0,
                MaxLevel = 2,
                BaseCost = 60000,
                CostMultiplier = 2.1,
                MonthlyMaintenance = 2800,
                EmissionReductionPercent = 0,
                CleanEnergyOutputMWh = 210,
                ForestAbsorptionTons = 0,
                PublicApprovalBonus = 11.0,
                IconSymbol = "🌋"
            });

            Upgrades.Add(new UpgradeItem
            {
                Id = "battery",
                Name = "Endüstriyel Batarya Enerji Deposu",
                Category = "İleri Teknoloji",
                Description = "Fazla rüzgar ve güneş elektriğini depolayarak gece saatlerinde şebekeyi besler.",
                Level = 0,
                MaxLevel = 3,
                BaseCost = 42000,
                CostMultiplier = 1.9,
                MonthlyMaintenance = 1900,
                EmissionReductionPercent = 0,
                CleanEnergyOutputMWh = 110,
                ForestAbsorptionTons = 0,
                PublicApprovalBonus = 7.0,
                IconSymbol = "🔋"
            });

            Upgrades.Add(new UpgradeItem
            {
                Id = "hydrogen",
                Name = "Yeşil Hidrojen Elektroliz Üssü",
                Category = "İleri Teknoloji",
                Description = "Temiz elektrikle sudan yeşil hidrojen üreterek maden makinelerine sıfır karbon yakıt sağlar.",
                Level = 0,
                MaxLevel = 2,
                BaseCost = 75000,
                CostMultiplier = 2.2,
                MonthlyMaintenance = 4000,
                EmissionReductionPercent = 0.12,
                CleanEnergyOutputMWh = 90,
                ForestAbsorptionTons = 0,
                PublicApprovalBonus = 15.0,
                IconSymbol = "🧪"
            });

            Upgrades.Add(new UpgradeItem
            {
                Id = "smartgrid",
                Name = "Akıllı Şebeke & Yapay Zeka Dengeleme",
                Category = "İleri Teknoloji",
                Description = "Yapay zeka ile enerji hatlarındaki iletim kayıplarını ve israfı önler.",
                Level = 0,
                MaxLevel = 2,
                BaseCost = 45000,
                CostMultiplier = 1.95,
                MonthlyMaintenance = 2100,
                EmissionReductionPercent = 0.08,
                CleanEnergyOutputMWh = 65,
                ForestAbsorptionTons = 0,
                PublicApprovalBonus = 8.0,
                IconSymbol = "📡"
            });

            Upgrades.Add(new UpgradeItem
            {
                Id = "evehicle",
                Name = "Elektrikli Maden Kamyonları & Makineleri",
                Category = "Doğa & Karbon Yutağı",
                Description = "Maden sahasındaki dizel iş makinelerini sıfır emisyonlu elektrikli modellere geçirir.",
                Level = 0,
                MaxLevel = 2,
                BaseCost = 39000,
                CostMultiplier = 1.9,
                MonthlyMaintenance = 1600,
                EmissionReductionPercent = 0.14,
                CleanEnergyOutputMWh = 0,
                ForestAbsorptionTons = 0,
                PublicApprovalBonus = 11.0,
                IconSymbol = "🚛"
            });
        }

        private void InitializeDilemmas()
        {
            // DILEMMA 1: Bakanlık Teftişi (4 Choices)
            DilemmaEvent d1 = new DilemmaEvent
            {
                Id = "dilemma_1",
                Title = "Bakanlık Çevre Müfettişi Teftişte!",
                CharacterName = "Müfettiş Selim Kaya",
                CharacterRole = "Çevre ve İklim Değişikliği Bakanlığı",
                Description = "Maden bacalarındaki kükürt ve sera gazı oranları yasal sınırın %45 üzerinde çıktı. Müfettiş teftiş tutanağını elinde tutuyor."
            };
            d1.Choices.Add(new DilemmaChoice
            {
                Key = "A",
                Title = "Radikal Çözüm: Tam Filtre Yenilemesi",
                Text = "Bütçeden kaynak ayırarak bacalara en modern filtreleri tak ve yeşil taahhütname imzala.",
                ConsequenceSummary = "-₺24,000 | Karbon -%18 | Halk +%14 | Güvenlik +%5",
                Cost = 24000,
                Revenue = 0,
                EmissionChangePercent = -0.18,
                ClimateThreatChange = -4.0,
                PublicApprovalChange = 14.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Müfettiş titiz yaklaşımınızı takdirle karşıladı. Tesis ceza almaktan kurtuldu ve çevre denetiminden tam not aldı."
            });
            d1.Choices.Add(new DilemmaChoice
            {
                Key = "B",
                Title = "Orta Yol: Kısmi Bakım ve Filtre Temizliği",
                Text = "Mevcut filtreleri tamir ettir ve kömür kazanlarının yükünü geçici olarak %15 kıs.",
                ConsequenceSummary = "-₺10,000 | Karbon -%8 | Halk +%6",
                Cost = 10000,
                Revenue = 0,
                EmissionChangePercent = -0.08,
                ClimateThreatChange = -1.5,
                PublicApprovalChange = 6.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Emisyonlar kısmen düştü, müfettiş süre verdi ancak kalıcı çözüm hala bekleniyor."
            });
            d1.Choices.Add(new DilemmaChoice
            {
                Key = "C",
                Title = "Kısa Yol: Cezayı Öde ve Kömüre Devam Et",
                Text = "Yatırım yapma, idari para cezasını ödeyip kömür üretimini aksatmadan devam ettir.",
                ConsequenceSummary = "-₺14,000 Ceza | Karbon Değişmez | Halk -%15",
                Cost = 14000,
                Revenue = 0,
                EmissionChangePercent = 0.05,
                ClimateThreatChange = 3.5,
                PublicApprovalChange = -15.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Ceza ödendi ama dumanlar şehri boğmaya devam ediyor. Çevre dernekleri dava açmaya hazırlanıyor."
            });
            d1.Choices.Add(new DilemmaChoice
            {
                Key = "D",
                Title = "Bürokratik İtiraz: Avukatlarla Süreci Uzat",
                Text = "Ölçüm cihazlarının kalibrasyonuna itiraz ederek mahkemeden yürütmeyi durdurma kararı iste.",
                ConsequenceSummary = "-₺6,000 Hukuk | Riskli! | Halk -%20 | Ceza Katlanabilir",
                Cost = 6000,
                Revenue = 0,
                EmissionChangePercent = 0.08,
                ClimateThreatChange = 5.0,
                PublicApprovalChange = -20.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Mahkeme itirazı reddetti ve bakanlık cezasını ikiye katladı! Halkın güveni dip yaptı."
            });
            Dilemmas.Add(d1);

            // DILEMMA 2: Yaz Sıcak Dalgası (3 Choices)
            DilemmaEvent d2 = new DilemmaEvent
            {
                Id = "dilemma_2",
                Title = "Yaz Sıcak Hava Dalgası & Şebeke Krizi",
                CharacterName = "Operatör Murat Demir",
                CharacterRole = "Ulusal Enerji İletim Şebekesi",
                Description = "Aşırı sıcaklar sebebiyle şehir genelinde klimalar trafoları patlatıyor. Şebeke sizden acil +200 MWh kömür yakmanızı talep ediyor."
            };
            d2.Choices.Add(new DilemmaChoice
            {
                Key = "A",
                Title = "Fırsatçı Üretim: Kazanları %140'a Zorla",
                Text = "Tüm kazanları maksimuma çıkar ve yüksek tarife üzerinden piyasaya elektrik sat.",
                ConsequenceSummary = "+₺36,000 Gelir | Karbon Salımı +%35 | İklim Tehdidi +%7 | Halk -%10",
                Cost = 0,
                Revenue = 36000,
                EmissionChangePercent = 0.35,
                ClimateThreatChange = 7.0,
                PublicApprovalChange = -10.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Kasa doldu ancak maden bacasından çıkan kara dumanlar gökyüzünü kapladı. Şehirde hava kalitesi 'Kırmızı Alarm' seviyesine fırladı!"
            });
            d2.Choices.Add(new DilemmaChoice
            {
                Key = "B",
                Title = "Hibrit Destek: Yenilenebilir Güçle Destekle",
                Text = "Güneş ve rüzgar santrallerini tam kapasite devreye al, kömürü dengeli tut.",
                ConsequenceSummary = "+₺18,000 Gelir | +50 MWh Temiz Güç | Halk +%8",
                Cost = 4000,
                Revenue = 22000,
                EmissionChangePercent = -0.05,
                ClimateThreatChange = -1.0,
                PublicApprovalChange = 8.0,
                CleanEnergyBonus = 50,
                ResultNarrative = "Akıllıca yönetim sayesinde şehir karanlıkta kalmadı ve hava kalitesi zehirlenmedi."
            });
            d2.Choices.Add(new DilemmaChoice
            {
                Key = "C",
                Title = "Tasarruf Çağrısı & Dengeli Dağıtım",
                Text = "Aşırı yüklenmeyi reddet, şehrin sanayi hatlarında planlı kesinti talep et.",
                ConsequenceSummary = "+₺6,000 Gelir | Sıfır Fazla Salım | Sanayi Tepkisi -%5",
                Cost = 0,
                Revenue = 6000,
                EmissionChangePercent = -0.10,
                ClimateThreatChange = -2.0,
                PublicApprovalChange = -3.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Kazanlar aşırı yıpranmaktan kurtuldu ancak sanayi kuruluşları elektrik yetersizliğinden şikayet etti."
            });
            Dilemmas.Add(d2);

            // DILEMMA 3: Aktivistler Nizamiyede (3 Choices)
            DilemmaEvent d3 = new DilemmaEvent
            {
                Id = "dilemma_3",
                Title = "Genç İklim Aktivistleri Maden Önünde!",
                CharacterName = "Ece Yılmaz",
                CharacterRole = "Gelecek İçin İklim İnisiyatifi",
                Description = "Yüzlerce üniversiteli genç 'Kömür Değil Temiz Gelecek!' pankartlarıyla maden girişini kapattı. Basın ve kameralar olay yerinde."
            };
            d3.Choices.Add(new DilemmaChoice
            {
                Key = "A",
                Title = "Diyalog & Yeşil Dönüşüm Paktı",
                Text = "Öğrencileri tesise davet et, şeffaf dönüşüm planını sun ve gençlerle fidan dikimi başlat.",
                ConsequenceSummary = "-₺16,000 Fon | Halk Desteği +%24 | Yeşil Prestij",
                Cost = 16000,
                Revenue = 0,
                EmissionChangePercent = -0.10,
                ClimateThreatChange = -3.0,
                PublicApprovalChange = 24.0,
                CleanEnergyBonus = 40,
                ResultNarrative = "Eylem bir çevre festivaline dönüştü. Ulusal basında 'Diyalogla Yeşeren Maden' başlıkları atıldı."
            });
            d3.Choices.Add(new DilemmaChoice
            {
                Key = "B",
                Title = "Basın Açıklaması & Kısmi Taahhüt",
                Text = "Sözcü gönder, kömür filtrelerinin artırılacağını açıkla, tansiyonu düşür.",
                ConsequenceSummary = "-₺5,000 İletişim | Halk Desteği +%8",
                Cost = 5000,
                Revenue = 0,
                EmissionChangePercent = -0.03,
                ClimateThreatChange = -0.5,
                PublicApprovalChange = 8.0,
                CleanEnergyBonus = 10,
                ResultNarrative = "Eylemciler dağıldı ancak gözler madenin verdiği sözleri tutup tutmayacağında."
            });
            d3.Choices.Add(new DilemmaChoice
            {
                Key = "C",
                Title = "Güvenlik Müdahalesi & Barikat",
                Text = "Kapıları kilitle, özel güvenlik çağır ve eylemcileri maden sahasından uzaklaştır.",
                ConsequenceSummary = "₺0 Masraf | Halk Desteği -%22 | Boykot Çağrısı!",
                Cost = 0,
                Revenue = 0,
                EmissionChangePercent = 0,
                ClimateThreatChange = 2.0,
                PublicApprovalChange = -22.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Görüntüler sosyal medyada infial yarattı. Şirket ürünlerine ve maden hisselerine boykot başladı."
            });
            Dilemmas.Add(d3);

            // DILEMMA 4: Metan Gazı Sızıntısı (3 Choices)
            DilemmaEvent d4 = new DilemmaEvent
            {
                Id = "dilemma_4",
                Title = "Derin Galeride Metan Gazı Sızıntısı!",
                CharacterName = "Kazım Usta",
                CharacterRole = "Maden Başmühendisi",
                Description = "3. galeride yoğun grizu ve metan gazı cebi yarıldı. Metan CO2'den 84 kat daha zararlı ve patlama riski taşıyor!"
            };
            d4.Choices.Add(new DilemmaChoice
            {
                Key = "A",
                Title = "Acil Kojenerasyon Ünitesi Kur",
                Text = "Vakum boruları döşeyerek gazı çek, yakıp temiz elektriğe dönüştür.",
                ConsequenceSummary = "-₺28,000 Yatırım | +90 MWh Elektrik | Güvenlik +%18",
                Cost = 28000,
                Revenue = 0,
                EmissionChangePercent = -0.12,
                ClimateThreatChange = -4.0,
                PublicApprovalChange = 12.0,
                CleanEnergyBonus = 90,
                ResultNarrative = "Büyük tehlike temiz enerjiye çevrildi! Metan sızıntısı atmosfere karışmadan elektrik şebekesine verildi."
            });
            d4.Choices.Add(new DilemmaChoice
            {
                Key = "B",
                Title = "Galeriyi Betonla Mühürle",
                Text = "Riskli damarı mühürle ve kömür üretimini başka sektöre kaydır.",
                ConsequenceSummary = "-₺12,000 Mühürleme | Kömür Kapasitesi -%10 | Güvenlik +%10",
                Cost = 12000,
                Revenue = 0,
                EmissionChangePercent = -0.05,
                ClimateThreatChange = -1.0,
                PublicApprovalChange = 4.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Galeri mühürlendi, can kaybı önlendi fakat o damardaki kömür rezervi terk edildi."
            });
            d4.Choices.Add(new DilemmaChoice
            {
                Key = "C",
                Title = "Doğrudan Atmosfere Tahliye Et",
                Text = "Fanları çalıştırıp gazı dağ havasına üfle, iş aksamasın.",
                ConsequenceSummary = "₺0 Maliyet | İklim Tehdidi +%12 | Ağır Sera Gazı Patlaması!",
                Cost = 0,
                Revenue = 0,
                EmissionChangePercent = 0.28,
                ClimateThreatChange = 12.0,
                PublicApprovalChange = -14.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Binlerce ton metan atmosfere karıştı. Bölgesel sıcaklık artışı hızlandı ve ozon tabakası zarar gördü."
            });
            Dilemmas.Add(d4);

            // DILEMMA 5: Yeşil İklim Fonu (3 Choices)
            DilemmaEvent d5 = new DilemmaEvent
            {
                Id = "dilemma_5",
                Title = "Uluslararası Yeşil İklim Fonu Hibesi",
                CharacterName = "Helena Vance",
                CharacterRole = "Avrupa İklim ve Dönüşüm Bankası",
                Description = "Madeninizi 'Karbon Nötr Model Park' ilan ederseniz ₺55,000 hibe teklif ediliyor. Ancak kömür üretimini %25 kısmak şart!"
            };
            d5.Choices.Add(new DilemmaChoice
            {
                Key = "A",
                Title = "Taahhütnameyi İmzala & Rüzgar/Güneşe Yatır",
                Text = "Hibeyi al, kömür kazanlarını kıs, dağa dev rüzgar ve güneş üssü kur.",
                ConsequenceSummary = "+₺55,000 Hibe | Karbon -%30 | Yenilenebilir +150 MWh",
                Cost = 0,
                Revenue = 55000,
                EmissionChangePercent = -0.30,
                ClimateThreatChange = -6.0,
                PublicApprovalChange = 20.0,
                CleanEnergyBonus = 150,
                ResultNarrative = "Tesisiniz uluslararası 'Yeşil Maden Dönüşüm Ödülü'ne aday gösterildi! Temiz enerji rekor kırdı."
            });
            d5.Choices.Add(new DilemmaChoice
            {
                Key = "B",
                Title = "Kısmi Ortaklık: Hibeyi Bataryaya Ayır",
                Text = "Şartları müzakere et, hibeyi enerji depolama ve şebeke modernizasyonuna yönlendir.",
                ConsequenceSummary = "+₺30,000 Hibe | Yenilenebilir +60 MWh | Karbon -%12",
                Cost = 0,
                Revenue = 30000,
                EmissionChangePercent = -0.12,
                ClimateThreatChange = -3.0,
                PublicApprovalChange = 10.0,
                CleanEnergyBonus = 60,
                ResultNarrative = "Batarya depolama tesisi devreye girdi, kömürden çıkış için dengeli bir adım atıldı."
            });
            d5.Choices.Add(new DilemmaChoice
            {
                Key = "C",
                Title = "Hibeyi Reddet: Fosil Güçle Kal",
                Text = "Dış müdahaleyi reddet, geleneksel kömür üretimine sadık kal.",
                ConsequenceSummary = "₺0 Değişim | Gelecek Kaçtı | Rakipler Öne Geçti",
                Cost = 0,
                Revenue = 0,
                EmissionChangePercent = 0.05,
                ClimateThreatChange = 2.0,
                PublicApprovalChange = -6.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Kömür devam etti ancak dünya temiz enerjiye geçerken madeniniz çağın gerisinde kaldı."
            });
            Dilemmas.Add(d5);

            // DILEMMA 6: Köyün Suyu Kirlendi (3 Choices)
            DilemmaEvent d6 = new DilemmaEvent
            {
                Id = "dilemma_6",
                Title = "Kızılpınar Köyü İçme Suyu Zehirlendi!",
                CharacterName = "Muhtar Hasan Demir",
                CharacterRole = "Kızılpınar Köyü Temsilcisi",
                Description = "Maden atık havuzundaki çatlak sebebiyle asitli ağır metal suyu köyün ana içme suyu pınarına karıştı!"
            };
            d6.Choices.Add(new DilemmaChoice
            {
                Key = "A",
                Title = "Tam Sorumluluk & Arıtma Tesisi Kur",
                Text = "Köye tankerlerle su taşı, kalıcı arıtma tesisi kur ve sağlık tazminatlarını öde.",
                ConsequenceSummary = "-₺32,000 Tazminat | Halk Desteği +%18 | İklim -%4",
                Cost = 32000,
                Revenue = 0,
                EmissionChangePercent = -0.06,
                ClimateThreatChange = -4.0,
                PublicApprovalChange = 18.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Köye modern su hattı bağlandı, arıtma tamamlandı. Muhtar basına 'Müdür sözünü tuttu' açıklaması yaptı."
            });
            d6.Choices.Add(new DilemmaChoice
            {
                Key = "B",
                Title = "Sadece Acil Su Dağıt ve Havuzu Sıva",
                Text = "Geçici tanker su sağla, çatlağı hızlı çimentoyla kapat.",
                ConsequenceSummary = "-₺12,000 Masraf | Halk Desteği -%5 | Geçici Çözüm",
                Cost = 12000,
                Revenue = 0,
                EmissionChangePercent = 0,
                ClimateThreatChange = 1.0,
                PublicApprovalChange = -5.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Geçici sıva yapıldı ancak yer altı sularında sızıntı riski hala sürüyor."
            });
            d6.Choices.Add(new DilemmaChoice
            {
                Key = "C",
                Title = "Sorumluluğu Reddet & Avukat Gönder",
                Text = "Kirliliğin madenle ilgisi olmadığını iddia et, köylüleri mahkemeye yönlendir.",
                ConsequenceSummary = "-₺6,000 Hukuk | Halk Desteği -%25 | Ağır Dava Riski!",
                Cost = 6000,
                Revenue = 0,
                EmissionChangePercent = 0.04,
                ClimateThreatChange = 4.0,
                PublicApprovalChange = -25.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Çevre savcılığı madene soruşturma başlattı. Bölge halkı maden kapısında protesto başlattı."
            });
            Dilemmas.Add(d6);

            // DILEMMA 7: Orman Yangını (3 Choices)
            DilemmaEvent d7 = new DilemmaEvent
            {
                Id = "d7",
                Title = "Orman Yangını Madene Yaklaşıyor!",
                CharacterName = "İtfaiye Amiri Yusuf Çelik",
                CharacterRole = "İl Yangın Koordinasyon Merkezi",
                Description = "Aşırı kuraklık yüzünden güney çamlığında büyük yangın çıktı. İtfaiye sizden iş makineleri ve dozer desteği istiyor."
            };
            d7.Choices.Add(new DilemmaChoice
            {
                Key = "A",
                Title = "Tüm Makineleri Yangına Gönder",
                Text = "Kömür çıkarmayı durdur, dozerleri yangın emniyet şeridi açmaya gönder.",
                ConsequenceSummary = "-₺18,000 Kayıp Üretim | 1,000 Hektar Orman Kurtuldu | Halk +%25",
                Cost = 18000,
                Revenue = 0,
                EmissionChangePercent = -0.15,
                ClimateThreatChange = -6.0,
                PublicApprovalChange = 25.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Maden dozerleri yangının önünü kesti! 1,000 hektar çamlık kurtarıldı. Vali bizzat madene teşekkür plaketi verdi."
            });
            d7.Choices.Add(new DilemmaChoice
            {
                Key = "B",
                Title = "Kısmi Destek: Sadece 2 Kamyon Gönder",
                Text = "Üretimi aksatmadan 2 su tankeri gönder.",
                ConsequenceSummary = "-₺6,000 Masraf | Halk Desteği +%5",
                Cost = 6000,
                Revenue = 0,
                EmissionChangePercent = 0.05,
                ClimateThreatChange = 2.0,
                PublicApprovalChange = 5.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Yangın güçlükle söndürüldü, ormanın bir kısmı yandı ancak maden tesisleri zarar görmedi."
            });
            d7.Choices.Add(new DilemmaChoice
            {
                Key = "C",
                Title = "Kendi Tesislerini Koru, Ormana Karışma",
                Text = "Makineleri maden sınırında beklet, yangınla itfaiye ilgilensin.",
                ConsequenceSummary = "₺0 Maliyet | İklim Tehdidi +%10 | Devasa Orman Kül Oldu!",
                Cost = 0,
                Revenue = 0,
                EmissionChangePercent = 0.20,
                ClimateThreatChange = 10.0,
                PublicApprovalChange = -18.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "3,000 hektar orman kül oldu! Yanan ağaçların saldığı devasa karbon bulutları atmosferi zehirledi."
            });
            Dilemmas.Add(d7);

            // DILEMMA 8: Karbon Vergisi Yasası (3 Choices)
            DilemmaEvent d8 = new DilemmaEvent
            {
                Id = "d8",
                Title = "Karbon Vergisi Meclisten Geçti!",
                CharacterName = "Maliye Uzmanı Dr. Elif Sarı",
                CharacterRole = "Gelir İdaresi Başkanlığı",
                Description = "Yeni yasa ile her ton CO2 salımı için ton başına ₺30 vergi uygulanacak. Ancak yenilenebilir enerji yatırımı yapanlara muafiyet var."
            };
            d8.Choices.Add(new DilemmaChoice
            {
                Key = "A",
                Title = "Güneş ve Rüzgara Yatırarak Muafiyet Al",
                Text = "Acil yenilenebilir protokolü hazırla, vergiden tamamen muaf ol.",
                ConsequenceSummary = "+₺25,000 Teşvik | Karbon -%20 | Yenilenebilir +100 MWh",
                Cost = 0,
                Revenue = 25000,
                EmissionChangePercent = -0.20,
                ClimateThreatChange = -4.0,
                PublicApprovalChange = 12.0,
                CleanEnergyBonus = 100,
                ResultNarrative = "Vergi muafiyeti alındı ve üstüne devletten yeşil teşvik kredisi çekildi!"
            });
            d8.Choices.Add(new DilemmaChoice
            {
                Key = "B",
                Title = "Karbon Vergisi Faturasını Öde",
                Text = "Sistemi değiştirme, her ay düzenli karbon vergisini öde.",
                ConsequenceSummary = "-₺28,000 Vergi Masrafı | Değişim Yok",
                Cost = 28000,
                Revenue = 0,
                EmissionChangePercent = 0.04,
                ClimateThreatChange = 2.0,
                PublicApprovalChange = -5.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Ağır karbon vergisi kasanızı eritti. Kömürün kar marjı sıfıra yaklaştı."
            });
            d8.Choices.Add(new DilemmaChoice
            {
                Key = "C",
                Title = "Kömür Lobisiyle Birlikte Dava Aç",
                Text = "Verginin sanayiyi batıracağını iddia ederek Anayasa Mahkemesi'ne başvur.",
                ConsequenceSummary = "-₺8,000 Dava | Halk Desteği -%16 | Belirsizlik",
                Cost = 8000,
                Revenue = 0,
                EmissionChangePercent = 0.05,
                ClimateThreatChange = 3.0,
                PublicApprovalChange = -16.0,
                CleanEnergyBonus = 0,
                ResultNarrative = "Dava reddedildi ve geriye dönük faiz cezalarıyla birlikte fatura ikiye katlandı."
            });
            Dilemmas.Add(d8);
        }

        private void InitializeClimateFacts()
        {
            ClimateFacts.Add(new ClimateFact
            {
                Title = "Kömürün Görünmeyen Bedeli",
                Body = "Kömür yakımı dünyada her yıl 800.000'den fazla insanın erken ölümüne yol açmakta ve sağlık harcamalarında trilyonlarca liralık zarara neden olmaktadır.",
                Source = "Dünya Sağlık Örgütü (WHO)",
                ActionCall = "Kömürü terk etmek sadece gezegeni değil, insan sağlığını da kurtarır."
            });

            ClimateFacts.Add(new ClimateFact
            {
                Title = "1.5°C Eşiği ve Geri Dönülemez Nokta",
                Body = "Küresel ortalama sıcaklık artışı 1.5°C'yi geçtiğinde Grönland ve Antarktika buz örtülerinin erimesi kontrol edilemez hale gelerek dünya genelinde kıyı kentlerini sular altında bırakacaktır.",
                Source = "IPCC 6. Değerlendirme Raporu",
                ActionCall = "2030 yılına kadar küresel emisyonları %45 düşürmek zorundayız."
            });

            ClimateFacts.Add(new ClimateFact
            {
                Title = "Metan: Süper Sera Gazı",
                Body = "Metan gazı ilk 20 yıllık zaman diliminde CO2'den 84 kat daha etkilidir. Kömür ocaklarında açığa çıkan metanı yakalayıp kojenerasyonla yakmak, anında iklim soğutma etkisi sağlar.",
                Source = "BM Çevre Programı (UNEP)",
                ActionCall = "Maden metanını elektriğe dönüştürmek en hızlı iklim önlemidir."
            });

            ClimateFacts.Add(new ClimateFact
            {
                Title = "Yenilenebilir Enerjinin Ekonomik Zaferi",
                Body = "Güneş ve rüzgar enerjisinin maliyeti son on yılda %85'ten fazla düştü. Bugün dünyada yeni bir kömür santrali işletmek, sıfırdan güneş santrali kurmaktan %30 daha pahalıdır.",
                Source = "Uluslararası Yenilenebilir Enerji Ajansı (IRENA)",
                ActionCall = "Yeşil enerji artık bir fedakarlık değil, ekonomik bir zorunluluktur."
            });
        }

        public void RecalculateStats()
        {
            double filterTotal = 0;
            double windMwh = 0;
            double solarMwh = 0;
            double methaneMwh = 0;
            double hydroMwh = 0;
            double geoMwh = 0;
            double batteryMwh = 0;
            double hydrogenMwh = 0;
            double forestAbsorb = 0;
            double ccsTotal = 0;
            double totalMaintenance = 0;

            for (int i = 0; i < Upgrades.Count; i++)
            {
                UpgradeItem up = Upgrades[i];
                if (up.Level > 0)
                {
                    totalMaintenance += up.MonthlyMaintenance * up.Level;

                    if (up.Id == "filter")
                        filterTotal += up.EmissionReductionPercent * up.Level;
                    else if (up.Id == "wind")
                        windMwh += up.CleanEnergyOutputMWh * up.Level;
                    else if (up.Id == "solar")
                        solarMwh += up.CleanEnergyOutputMWh * up.Level;
                    else if (up.Id == "methane")
                    {
                        filterTotal += up.EmissionReductionPercent * up.Level;
                        methaneMwh += up.CleanEnergyOutputMWh * up.Level;
                    }
                    else if (up.Id == "forest")
                        forestAbsorb += up.ForestAbsorptionTons * up.Level;
                    else if (up.Id == "ccs")
                        ccsTotal += up.EmissionReductionPercent * up.Level;
                    else if (up.Id == "hydro")
                        hydroMwh += up.CleanEnergyOutputMWh * up.Level;
                    else if (up.Id == "geothermal")
                        geoMwh += up.CleanEnergyOutputMWh * up.Level;
                    else if (up.Id == "battery")
                        batteryMwh += up.CleanEnergyOutputMWh * up.Level;
                    else if (up.Id == "hydrogen")
                    {
                        filterTotal += up.EmissionReductionPercent * up.Level;
                        hydrogenMwh += up.CleanEnergyOutputMWh * up.Level;
                    }
                    else if (up.Id == "smartgrid")
                        filterTotal += up.EmissionReductionPercent * up.Level;
                    else if (up.Id == "evehicle")
                        filterTotal += up.EmissionReductionPercent * up.Level;
                }
            }

            FilterEfficiency = Math.Min(0.75, filterTotal);
            CcsEfficiency = Math.Min(0.88, ccsTotal);
            ForestAbsorptionRate = forestAbsorb;

            RenewableEnergyOutput = windMwh + solarMwh + methaneMwh + hydroMwh + geoMwh + batteryMwh + hydrogenMwh;

            // Coal energy output based on capacity slider and available coal
            double targetCoal = CoalCapacityMax * CoalProductionSlider;
            if (RawCoalStockTons < 20.0)
            {
                // Coal shortage throttles production!
                targetCoal = targetCoal * Math.Max(0.2, RawCoalStockTons / 20.0);
            }
            CoalEnergyOutput = targetCoal;
            TotalEnergyProduced = CoalEnergyOutput + RenewableEnergyOutput;

            // Gross Carbon
            GrossCarbonEmissionRate = CoalEnergyOutput * BaseCoalEmissionFactor;

            // After Filters & CCS
            double afterFilter = GrossCarbonEmissionRate * (1.0 - FilterEfficiency);
            double afterCcs = afterFilter * (1.0 - CcsEfficiency);
            NetCarbonEmissions = Math.Max(0, afterCcs - ForestAbsorptionRate);

            if (NetCarbonEmissions < LowestNetCarbonRecord)
            {
                LowestNetCarbonRecord = NetCarbonEmissions;
            }

            double renewShare = TotalEnergyProduced > 0 ? (RenewableEnergyOutput / TotalEnergyProduced) * 100.0 : 0;
            if (renewShare > PeakRenewablePercent)
            {
                PeakRenewablePercent = renewShare;
            }

            // FINANCIAL REVENUE (Contract Delivery & Spot Sales)
            double contractRevenue = 0.0;
            ContractPenaltyIncurred = 0.0;

            // Contract 1: Municipality (450 MWh)
            if (ContractMunicipalityActive)
            {
                if (TotalEnergyProduced >= 450.0)
                {
                    contractRevenue += 52000.0;
                }
                else
                {
                    // Failed to meet municipality quota!
                    ContractPenaltyIncurred += 14000.0;
                    PublicApproval = Math.Max(0, PublicApproval - 4.0);
                }
            }

            // Contract 2: Heavy Industry (300 MWh)
            if (ContractIndustryActive)
            {
                if (TotalEnergyProduced >= 750.0) // 450 + 300
                {
                    contractRevenue += 40000.0;
                }
                else if (TotalEnergyProduced >= 300.0 && !ContractMunicipalityActive)
                {
                    contractRevenue += 40000.0;
                }
                else
                {
                    ContractPenaltyIncurred += 10000.0;
                }
            }

            // Extra energy sold to spot market automatically at baseline
            double excessEnergy = Math.Max(0, TotalEnergyProduced - 750.0);
            double spotRevenue = excessEnergy * SpotEnergyPricePerMWh;

            MonthlyRevenue = contractRevenue + spotRevenue;

            // EXPENSES (Hardened Economy)
            double coalOpex = CoalEnergyOutput * 38.0;
            MonthlyCarbonTax = NetCarbonEmissions * CarbonTaxRatePerTon; // CARBON TAX!
            MonthlyExpenses = BaseOperationalExpense + coalOpex + totalMaintenance + MonthlyCarbonTax + ContractPenaltyIncurred;
        }

        public void NextMonthTick()
        {
            if (GameOver) return;

            RecalculateStats();

            // Deplete raw coal stock based on coal production
            double coalBurnedTons = (CoalEnergyOutput / CoalCapacityMax) * 35.0;
            RawCoalStockTons = Math.Max(0, RawCoalStockTons - coalBurnedTons);

            // Worker stamina natural recovery
            WorkerStamina = Math.Min(100.0, WorkerStamina + 8.0);

            // Cashflow
            double netMonthlyCashflow = MonthlyRevenue - MonthlyExpenses;
            Budget += netMonthlyCashflow;
            if (netMonthlyCashflow > 0) TotalProfitsMade += netMonthlyCashflow;

            // Carbon accumulation & warming
            TotalEmittedCarbon += NetCarbonEmissions;
            double tempIncrement = (NetCarbonEmissions / 5500.0) * 0.014;
            GlobalWarmingTemp += tempIncrement;

            // Climate threat scale
            ClimateThreatPercent = Math.Min(100.0, Math.Max(0.0, (GlobalWarmingTemp - 1.0) * 80.0 + (NetCarbonEmissions / 1100.0) * 22.0));

            // Public approval dynamics
            if (NetCarbonEmissions > 800)
            {
                PublicApproval = Math.Max(0, PublicApproval - 2.5);
            }
            else if (NetCarbonEmissions < 200)
            {
                PublicApproval = Math.Min(100, PublicApproval + 2.0);
            }

            if (RenewableEnergyOutput > EnergyDemand * 0.5)
            {
                PublicApproval = Math.Min(100, PublicApproval + 1.5);
            }

            // Fluctuate spot energy price randomly (₺95 to ₺155)
            Random r = new Random();
            SpotEnergyPricePerMWh = 95.0 + r.NextDouble() * 60.0;

            // Record history
            CarbonHistory.Add(NetCarbonEmissions);
            TempHistory.Add(GlobalWarmingTemp);
            double renewShare = TotalEnergyProduced > 0 ? (RenewableEnergyOutput / TotalEnergyProduced) * 100.0 : 0;
            RenewablePercentHistory.Add(renewShare);
            CarbonTaxHistory.Add(MonthlyCarbonTax);

            if (CarbonHistory.Count > 24) CarbonHistory.RemoveAt(0);
            if (TempHistory.Count > 24) TempHistory.RemoveAt(0);
            if (RenewablePercentHistory.Count > 24) RenewablePercentHistory.RemoveAt(0);
            if (CarbonTaxHistory.Count > 24) CarbonTaxHistory.RemoveAt(0);

            // Seasonal energy demand
            BaseEnergyDemand += 6.0;
            int monthOfYear = ((CurrentMonth - 1) % 12) + 1;
            double seasonalMult = 1.0;
            if (monthOfYear == 12 || monthOfYear == 1 || monthOfYear == 2) seasonalMult = 1.20;
            else if (monthOfYear == 6 || monthOfYear == 7 || monthOfYear == 8) seasonalMult = 1.16;
            EnergyDemand = BaseEnergyDemand * seasonalMult;

            // Dilemma triggers check
            CheckDilemmaTriggers();

            // Check achievement milestones
            CheckAchievements();

            CurrentMonth++;

            CheckGameEndConditions();
        }

        private void CheckDilemmaTriggers()
        {
            if (CurrentActiveDilemma != null) return;

            int dilemmaIndex = -1;
            if (CurrentMonth == 2) dilemmaIndex = 0;
            else if (CurrentMonth == 6) dilemmaIndex = 1;
            else if (CurrentMonth == 10) dilemmaIndex = 2;
            else if (CurrentMonth == 14) dilemmaIndex = 3;
            else if (CurrentMonth == 18) dilemmaIndex = 4;
            else if (CurrentMonth == 22) dilemmaIndex = 5;
            else if (CurrentMonth == 26) dilemmaIndex = 6;
            else if (CurrentMonth == 30) dilemmaIndex = 7;

            if (dilemmaIndex >= 0 && dilemmaIndex < Dilemmas.Count)
            {
                if (!Dilemmas[dilemmaIndex].Triggered)
                {
                    CurrentActiveDilemma = Dilemmas[dilemmaIndex];
                    Dilemmas[dilemmaIndex].Triggered = true;
                    Dilemmas[dilemmaIndex].TriggeredMonth = CurrentMonth;
                    DilemmaPendingPopup = true;
                    SoundEngine.PlayDilemma();
                }
            }
        }

        public void ApplyDilemmaChoiceByKey(string choiceKey)
        {
            if (CurrentActiveDilemma == null) return;

            DilemmaChoice choice = null;
            for (int i = 0; i < CurrentActiveDilemma.Choices.Count; i++)
            {
                if (CurrentActiveDilemma.Choices[i].Key == choiceKey)
                {
                    choice = CurrentActiveDilemma.Choices[i];
                    break;
                }
            }

            if (choice == null && CurrentActiveDilemma.Choices.Count > 0)
            {
                choice = CurrentActiveDilemma.Choices[0];
            }

            if (choice != null)
            {
                Budget -= choice.Cost;
                Budget += choice.Revenue;
                PublicApproval = Math.Max(0, Math.Min(100, PublicApproval + choice.PublicApprovalChange));
                ClimateThreatPercent = Math.Max(0, Math.Min(100, ClimateThreatPercent + choice.ClimateThreatChange));

                if (choice.CleanEnergyBonus > 0)
                {
                    RenewableEnergyOutput += choice.CleanEnergyBonus;
                }

                if (choice.EmissionChangePercent != 0)
                {
                    BaseCoalEmissionFactor = Math.Max(0.4, BaseCoalEmissionFactor * (1.0 + choice.EmissionChangePercent));
                }

                CurrentActiveDilemma.SelectedChoiceKey = choice.Key;
                PastResolvedDilemmas.Add(CurrentActiveDilemma);
            }

            CurrentActiveDilemma = null;
            DilemmaPendingPopup = false;
            RecalculateStats();
            SoundEngine.PlayClick();
        }

        // ==========================================
        // ACTIVE DIRECTOR ACTIONS (MANUAL WORK REQUIRED)
        // ==========================================
        public string ActionMineCoalShift()
        {
            if (WorkerStamina < 15.0)
            {
                return "İşçiler çok yorgun! (Yorgunluk dinlenmeli)";
            }

            double miningCost = 1200.0;
            if (Budget < miningCost) return "Yetersiz nakit! Vardiya için ₺1,200 gerekir.";

            Budget -= miningCost;
            RawCoalStockTons += 45.0; // Adds 45 tons of raw coal
            WorkerStamina = Math.Max(0, WorkerStamina - 18.0);
            MineSafety = Math.Max(10, MineSafety - 2.0);
            TotalCoalMinedByClick += 45;

            RecalculateStats();
            CheckAchievements();
            SoundEngine.PlayCash();
            return "+45 Ton Ham Kömür Çıkarıldı! (Stoka Eklendi, -₺1,200)";
        }

        public string ActionSellSpotEnergy()
        {
            // Sell 80 MWh to spot market manually
            if (TotalEnergyProduced < 100.0)
            {
                return "Satacak yeterli enerji üretilmiyor!";
            }

            double revenue = 80.0 * SpotEnergyPricePerMWh;
            Budget += revenue;
            SoundEngine.PlayCash();
            return string.Format("Spot Piyasaya 80 MWh Enerji Satıldı! (+₺{0:N0})", revenue);
        }

        public string ActionPlantTrees()
        {
            double cost = 4000.0;
            if (Budget < cost) return "Yetersiz bütçe! Ağaçlandırma için ₺4,000 gerekli.";
            Budget -= cost;
            ForestAbsorptionRate += 20.0;
            PublicApproval = Math.Min(100, PublicApproval + 4.0);
            TotalTreesPlantedByAction += 400;
            TotalEcoActionsCount++;
            RecalculateStats();
            CheckAchievements();
            SoundEngine.PlayEcoAction();
            return "+400 Fidan Dikildi! (Karbon Yutağı: +20 t, Halk: +%4)";
        }

        public string ActionChemicalScrub()
        {
            double cost = 2800.0;
            if (Budget < cost) return "Yetersiz bütçe! Baca kimyasalı için ₺2,800 gerekli.";
            Budget -= cost;
            NetCarbonEmissions = Math.Max(0, NetCarbonEmissions - 40.0);
            TotalEcoScrubbedTons += 40.0;
            TotalEcoActionsCount++;
            SoundEngine.PlayEcoAction();
            return "Bacalar Kimyasal Yıkandı! (-40 ton CO₂ Anlık Azalma)";
        }

        public string ActionWorkerBonus()
        {
            double cost = 3500.0;
            if (Budget < cost) return "Yetersiz bütçe! İkramiye için ₺3,500 gerekli.";
            Budget -= cost;
            WorkerStamina = Math.Min(100, WorkerStamina + 35.0);
            MineSafety = Math.Min(100, MineSafety + 8.0);
            PublicApproval = Math.Min(100, PublicApproval + 5.0);
            SoundEngine.PlayCash();
            return "İşçilere Prim & Dinlenme Verildi! (Kondisyon: +%35, Güvenlik: +%8)";
        }

        public string ActionPressConference()
        {
            double cost = 1800.0;
            if (Budget < cost) return "Yetersiz bütçe! Basın toplantısı için ₺1,800 gerekli.";
            Budget -= cost;
            PublicApproval = Math.Min(100, PublicApproval + 8.0);
            SoundEngine.PlayClick();
            return "Basın Toplantısı Yapıldı! (Halk Desteği: +%8)";
        }

        public string ActionInspectRenewables()
        {
            RenewableEnergyOutput += 40.0;
            PublicApproval = Math.Min(100, PublicApproval + 1.5);
            TotalEcoActionsCount++;
            SoundEngine.PlayUpgrade();
            return "Yenilenebilir Tesisler Optimize Edildi! (+40 MWh Temiz Güç)";
        }

        public void ManualScrubCarbon()
        {
            double scrubAmount = 15.0;
            TotalEcoScrubbedTons += scrubAmount;
            TotalEcoActionsCount++;
            NetCarbonEmissions = Math.Max(0, NetCarbonEmissions - scrubAmount);
            Budget += 45.0;
            SoundEngine.PlayEcoAction();
        }

        // ==========================================
        // PROACTIVE CRISIS PREVENTION ACTIONS & RISK CALCULATORS
        // ==========================================
        public double GetMinistryInspectionRisk()
        {
            return Math.Max(0.0, Math.Min(100.0, (NetCarbonEmissions / 950.0) * 55.0 + (100.0 - PublicApproval) * 0.45));
        }

        public double GetWorkerStrikeRisk()
        {
            return Math.Max(0.0, Math.Min(100.0, (100.0 - WorkerStamina) * 0.5 + (100.0 - MineSafety) * 0.5));
        }

        public double GetPublicProtestRisk()
        {
            double threatExtra = ClimateThreatPercent > 60.0 ? 25.0 : 0.0;
            return Math.Max(0.0, Math.Min(100.0, (100.0 - PublicApproval) * 0.80 + threatExtra));
        }

        public double GetBankruptcyRisk()
        {
            if (Budget >= 35000.0) return 5.0;
            if (Budget <= 0.0) return 100.0;
            return Math.Max(5.0, Math.Min(100.0, ((35000.0 - Budget) / 35000.0) * 100.0));
        }

        public double GetMinedustDisasterRisk()
        {
            double overload = CoalProductionSlider > 1.15 ? 25.0 : 0.0;
            return Math.Max(0.0, Math.Min(100.0, (100.0 - MineSafety) * 0.75 + overload));
        }

        public string ActionProactiveEnvironmentalAudit()
        {
            double cost = 3500.0;
            if (Budget < cost) return "Yetersiz bütçe! Çevre denetimi için ₺3,500 gerekli.";
            Budget -= cost;
            PublicApproval = Math.Min(100.0, PublicApproval + 6.0);
            ClimateThreatPercent = Math.Max(0.0, ClimateThreatPercent - 3.0);
            TotalEcoActionsCount++;
            SoundEngine.PlayEcoAction();
            return "Kapsamlı Çevre Denetimi Yaptırıldı! (Bakanlık riski düşürüldü, Halk: +%6)";
        }

        public string ActionProactiveUnionBargaining()
        {
            double cost = 4200.0;
            if (Budget < cost) return "Yetersiz bütçe! Sendika görüşmeleri ve sosyal paket için ₺4,200 gerekli.";
            Budget -= cost;
            WorkerStamina = Math.Min(100.0, WorkerStamina + 25.0);
            MineSafety = Math.Min(100.0, MineSafety + 14.0);
            PublicApproval = Math.Min(100.0, PublicApproval + 5.0);
            SoundEngine.PlayCash();
            return "Sendika Toplu Sözleşmesi İmzalandı! (Grev riski sıfırlandı, İSG: +%14)";
        }

        public string ActionProactiveEmergencyDrill()
        {
            double cost = 2000.0;
            if (Budget < cost) return "Yetersiz bütçe! Güvenlik tatbikatı için ₺2,000 gerekli.";
            Budget -= cost;
            MineSafety = Math.Min(100.0, MineSafety + 12.0);
            SoundEngine.PlayUpgrade();
            return "Grizu & Acil Tahliye Tatbikatı Tamamlandı! (Ocak Güvenliği: +%12)";
        }

        public bool SaveToFile(string path)
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("PlayerName=" + PlayerName);
                lines.Add("CurrentMonth=" + CurrentMonth);
                lines.Add("Difficulty=" + Difficulty);
                lines.Add("Budget=" + Budget.ToString("F2"));
                lines.Add("RawCoalStockTons=" + RawCoalStockTons.ToString("F2"));
                lines.Add("WorkerStamina=" + WorkerStamina.ToString("F2"));
                lines.Add("MineSafety=" + MineSafety.ToString("F2"));
                lines.Add("PublicApproval=" + PublicApproval.ToString("F2"));
                lines.Add("GlobalWarmingTemp=" + GlobalWarmingTemp.ToString("F4"));
                lines.Add("ClimateThreatPercent=" + ClimateThreatPercent.ToString("F2"));
                lines.Add("TotalEmittedCarbon=" + TotalEmittedCarbon.ToString("F2"));
                lines.Add("CoalProductionSlider=" + CoalProductionSlider.ToString("F2"));
                lines.Add("TotalTreesPlantedByAction=" + TotalTreesPlantedByAction);
                lines.Add("TotalCoalMinedByClick=" + TotalCoalMinedByClick);

                for (int i = 0; i < Upgrades.Count; i++)
                {
                    lines.Add("Upgrade_" + Upgrades[i].Id + "=" + Upgrades[i].Level);
                }

                for (int i = 0; i < Achievements.Count; i++)
                {
                    lines.Add("Ach_" + Achievements[i].Id + "=" + Achievements[i].Unlocked + ";" + Achievements[i].UnlockedMonth + ";" + Achievements[i].CurrentProgress.ToString("F2"));
                }

                File.WriteAllLines(path, lines.ToArray());
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static GameModel LoadFromFile(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string[] lines = File.ReadAllLines(path);
                GameModel m = new GameModel();

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();

                    if (key == "PlayerName") m.PlayerName = val;
                    else if (key == "CurrentMonth") int.TryParse(val, out m.CurrentMonth);
                    else if (key == "Difficulty") int.TryParse(val, out m.Difficulty);
                    else if (key == "Budget") double.TryParse(val, out m.Budget);
                    else if (key == "RawCoalStockTons") double.TryParse(val, out m.RawCoalStockTons);
                    else if (key == "WorkerStamina") double.TryParse(val, out m.WorkerStamina);
                    else if (key == "MineSafety") double.TryParse(val, out m.MineSafety);
                    else if (key == "PublicApproval") double.TryParse(val, out m.PublicApproval);
                    else if (key == "GlobalWarmingTemp") double.TryParse(val, out m.GlobalWarmingTemp);
                    else if (key == "ClimateThreatPercent") double.TryParse(val, out m.ClimateThreatPercent);
                    else if (key == "TotalEmittedCarbon") double.TryParse(val, out m.TotalEmittedCarbon);
                    else if (key == "CoalProductionSlider") double.TryParse(val, out m.CoalProductionSlider);
                    else if (key == "TotalTreesPlantedByAction") int.TryParse(val, out m.TotalTreesPlantedByAction);
                    else if (key == "TotalCoalMinedByClick") int.TryParse(val, out m.TotalCoalMinedByClick);
                    else if (key.StartsWith("Upgrade_"))
                    {
                        string upId = key.Substring("Upgrade_".Length);
                        int lvl = 0;
                        if (int.TryParse(val, out lvl))
                        {
                            UpgradeItem up = m.FindUpgrade(upId);
                            if (up != null) up.Level = lvl;
                        }
                    }
                    else if (key.StartsWith("Ach_"))
                    {
                        string achId = key.Substring("Ach_".Length);
                        string[] parts = val.Split(';');
                        for (int a = 0; a < m.Achievements.Count; a++)
                        {
                            if (m.Achievements[a].Id == achId)
                            {
                                if (parts.Length > 0) bool.TryParse(parts[0], out m.Achievements[a].Unlocked);
                                if (parts.Length > 1) int.TryParse(parts[1], out m.Achievements[a].UnlockedMonth);
                                if (parts.Length > 2) double.TryParse(parts[2], out m.Achievements[a].CurrentProgress);
                                break;
                            }
                        }
                    }
                }

                m.RecalculateStats();
                return m;
            }
            catch
            {
                return null;
            }
        }

        public bool BuyUpgrade(string upgradeId)
        {
            UpgradeItem item = null;
            for (int i = 0; i < Upgrades.Count; i++)
            {
                if (Upgrades[i].Id == upgradeId)
                {
                    item = Upgrades[i];
                    break;
                }
            }
            if (item == null || item.IsMaxed()) return false;

            double cost = item.GetCurrentCost();
            if (Budget < cost) return false;

            Budget -= cost;
            item.Level++;
            PublicApproval = Math.Min(100, PublicApproval + item.PublicApprovalBonus);
            TotalEcoActionsCount++;
            RecalculateStats();
            SoundEngine.PlayUpgrade();
            return true;
        }

        private void CheckGameEndConditions()
        {
            // Bankruptcy (Hardened: failure at -₺45,000)
            if (Budget < -45000)
            {
                GameOver = true;
                Victory = false;
                VictoryTier = "İFLAS";
                GameOverReason = "İFLAS: Maden işletmesi karbon vergileri ve işletme giderleri altında ezilerek iflas etti. Kayyum atandı.";
                SoundEngine.PlayDefeat();
                return;
            }

            // Ecological Collapse
            if (ClimateThreatPercent >= 98.0 || GlobalWarmingTemp >= 2.20)
            {
                GameOver = true;
                Victory = false;
                VictoryTier = "EKOLOJİK ÇÖKÜŞ";
                GameOverReason = "EKOLOJİK ÇÖKÜŞ: Kontrolsüz sera gazı salımı bölgeyi kuraklık, asit yağmuru ve zehirli duman felaketine sürükledi. Bölge tahliye edildi.";
                SoundEngine.PlayDefeat();
                return;
            }

            // Public Uprising
            if (PublicApproval <= 8.0)
            {
                GameOver = true;
                Victory = false;
                VictoryTier = "HALK İSYANI";
                GameOverReason = "HALK İSYANI: Zehirli hava ve halk sağlığı ihlalleri nedeniyle bölge halkı madeni kapattırdı.";
                SoundEngine.PlayDefeat();
                return;
            }

            // 60-Month Mandate Complete (Full Game Ending)
            if (CurrentMonth > MaxMonths)
            {
                GameOver = true;
                double renewShare = TotalEnergyProduced > 0 ? (RenewableEnergyOutput / TotalEnergyProduced) * 100.0 : 0;

                if (renewShare >= 75.0 && NetCarbonEmissions < 150.0 && PublicApproval >= 80.0)
                {
                    Victory = true;
                    VictoryTier = "PLATİN LİDER";
                    GameOverReason = "🏆 PLATİN YEŞİL DÖNÜŞÜM ZAFERİ! Madeni sıfır karbonlu 'Geleceğin Eko-Enerji Parkı'na dönüştürdünüz! Gezegen kurtuldu, tüm dünyaya örnek oldunuz!";
                    SoundEngine.PlayVictory();
                }
                else if (renewShare >= 55.0 && NetCarbonEmissions < 350.0 && PublicApproval >= 65.0)
                {
                    Victory = true;
                    VictoryTier = "ALTIN LİDER";
                    GameOverReason = "🥇 ALTIN BAŞARI! Kömür bağımlılığını kırarak şehrin enerjisinin çoğunu yenilenebilir kaynaklardan sağladınız. Temiz gökyüzü geri döndü!";
                    SoundEngine.PlayVictory();
                }
                else if (renewShare >= 35.0 && NetCarbonEmissions < 600.0)
                {
                    Victory = true;
                    VictoryTier = "GÜMÜŞ DÖNÜŞÜM";
                    GameOverReason = "🥈 GÜMÜŞ DÖNÜŞÜM: Maden işletmesini karlı tuttunuz ve temiz enerjiye geçişi başlattınız. Hedeflerin bir kısmına ulaşıldı.";
                    SoundEngine.PlayVictory();
                }
                else
                {
                    Victory = false;
                    VictoryTier = "BRONZ / BAŞARISIZ";
                    GameOverReason = "GÖREV SÜRESİ DOLDU: 5 yıllık sürede kömürden çıkış hedefleri yakalanamadı. Ağır karbon vergileri ve iklim krizi geleceği tehdit ediyor.";
                    SoundEngine.PlayDefeat();
                }
            }
        }

        public void AddEvent(string msg)
        {
            LastEventMessage = msg;
            EventLog.Add(string.Format("[Ay {0}] {1}", CurrentMonth, msg));
            if (EventLog.Count > 50) EventLog.RemoveAt(0);
        }

        public string GetCurrentDateString()
        {
            int year = StartYear + (CurrentMonth - 1) / 12;
            int monthOfYear = ((CurrentMonth - 1) % 12) + 1;
            string[] monthNames = new string[] { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
            return string.Format("{0} {1}", monthNames[monthOfYear - 1], year);
        }
    }
}
