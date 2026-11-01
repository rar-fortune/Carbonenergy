using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CarbonEnergy
{
    // ==========================================
    // DOUBLE-BUFFERED PANEL
    // ==========================================
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }
    }

    // ==========================================
    // GAME SCREENS
    // ==========================================
    public enum GameScreen
    {
        MainMenu,
        Settings,
        HowToPlay,
        NameInput,
        Playing,
        GameOver
    }

    // ==========================================
    // CRISIS DIALOG POPUP MODAL (SUPPORTS 3-4 CHOICES)
    // ==========================================
    public class CrisisDialogForm : Form
    {
        public string SelectedChoiceKey = "A";

        public CrisisDialogForm(DilemmaEvent dilemma)
        {
            this.Text = "🚨 KRİZ MASASI KARARI - " + dilemma.Title;
            int choiceCount = dilemma.Choices.Count;
            int calculatedHeight = 220 + choiceCount * 68;
            this.ClientSize = new Size(740, calculatedHeight);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(20, 24, 32);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            // Red warning banner
            Panel banner = new Panel();
            banner.Dock = DockStyle.Top;
            banner.Height = 46;
            banner.BackColor = Color.FromArgb(192, 57, 43);

            Label lblBanner = new Label();
            lblBanner.Text = "⚠️ KRİTİK İDARİ & İKLİM KARARI GEREKTİREN DURUM";
            lblBanner.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblBanner.ForeColor = Color.White;
            lblBanner.AutoSize = true;
            lblBanner.Location = new Point(14, 12);
            banner.Controls.Add(lblBanner);
            this.Controls.Add(banner);

            Label lblChar = new Label();
            lblChar.Text = string.Format("Makamınıza İleten: {0} ({1})", dilemma.CharacterName, dilemma.CharacterRole);
            lblChar.Font = new Font("Segoe UI", 9.2f, FontStyle.Bold);
            lblChar.ForeColor = Color.FromArgb(52, 152, 219);
            lblChar.Location = new Point(25, 58);
            lblChar.Size = new Size(690, 22);
            this.Controls.Add(lblChar);

            Label lblTitle = new Label();
            lblTitle.Text = dilemma.Title;
            lblTitle.Font = new Font("Segoe UI", 13.5f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(241, 196, 15);
            lblTitle.Location = new Point(25, 80);
            lblTitle.Size = new Size(690, 28);
            this.Controls.Add(lblTitle);

            Label lblDesc = new Label();
            lblDesc.Text = dilemma.Description;
            lblDesc.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblDesc.ForeColor = Color.FromArgb(215, 220, 230);
            lblDesc.Location = new Point(25, 114);
            lblDesc.Size = new Size(690, 54);
            this.Controls.Add(lblDesc);

            // Choice Buttons (A, B, C, D)
            Color[] borderColors = new Color[] {
                Color.FromArgb(46, 204, 113),
                Color.FromArgb(52, 152, 219),
                Color.FromArgb(231, 76, 60),
                Color.FromArgb(155, 89, 182)
            };

            for (int i = 0; i < choiceCount; i++)
            {
                DilemmaChoice ch = dilemma.Choices[i];
                Button btn = new Button();
                btn.Text = string.Format("{0}) {1} : {2}\n[{3}]", ch.Key, ch.Title, ch.Text, ch.ConsequenceSummary);
                btn.Location = new Point(25, 180 + i * 66);
                btn.Size = new Size(690, 58);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = borderColors[i % borderColors.Length];
                btn.FlatAppearance.BorderSize = 1;
                btn.BackColor = Color.FromArgb(30, 35, 46);
                btn.ForeColor = Color.White;
                btn.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
                btn.Cursor = Cursors.Hand;
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.Padding = new Padding(10, 0, 0, 0);
                btn.Tag = ch.Key;

                btn.MouseEnter += new EventHandler(delegate(object s, EventArgs ev)
                {
                    Button b = s as Button;
                    b.BackColor = Color.FromArgb(50, 60, 75);
                });
                btn.MouseLeave += new EventHandler(delegate(object s, EventArgs ev)
                {
                    Button b = s as Button;
                    b.BackColor = Color.FromArgb(30, 35, 46);
                });
                btn.Click += new EventHandler(delegate(object s, EventArgs ev)
                {
                    Button b = s as Button;
                    SelectedChoiceKey = b.Tag.ToString();
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                });

                this.Controls.Add(btn);
            }
        }
    }

    // ==========================================
    // MAIN GAME FORM
    // ==========================================
    public class GameForm : Form
    {
        private GameModel model;
        private WorldRenderer renderer;
        private GameScreen currentScreen = GameScreen.MainMenu;

        // Timers
        private Timer renderTimer;
        private Timer monthTimer;
        private float menuAnimPhase = 0f;

        // ===== MENU SCREEN CONTROLS =====
        private DoubleBufferedPanel menuPanel;
        private Button btnMenuPlay;
        private Button btnMenuLoad;
        private Button btnMenuHowToPlay;
        private Button btnMenuSettings;
        private Button btnMenuExit;

        // ===== HOW TO PLAY SCREEN CONTROLS =====
        private Panel howToPlayPanel;
        private Panel guideCard;
        private Button btnHowToPlayBack;
        private Button btnHowToPlayStart;
        private Label lblGuideSectionTitle;
        private Label lblGuideSectionBadge;
        private TextBox txtGuideContent;
        private Button[] guideCategoryBtns;
        private int currentGuideTab = 0;

        // ===== SETTINGS SCREEN CONTROLS =====
        private Panel settingsPanel;
        private Panel settingsCard;
        private Label lblSettingsTitle;
        private Panel lineDecor;
        private CheckBox chkSoundEnabled;
        private TrackBar tbVolume;
        private Label lblVolumeVal;
        private ComboBox cmbDifficulty;
        private ComboBox cmbGameSpeed;
        private Button btnSettingsBack;

        // ===== NAME INPUT SCREEN CONTROLS =====
        private Panel namePanel;
        private Panel nameCard;
        private TextBox txtPlayerName;
        private Button btnStartGame;
        private Button btnNameBack;
        private Label lblNamePrompt;

        // ===== GAME HUD SCREEN CONTROLS =====
        private Panel gameContainer;
        private Panel headerPanel;
        private DoubleBufferedPanel canvasPanel;
        private Panel tabNavPanel;
        private Panel contentPanel;

        // Tab sub-panels
        private Panel panelOperations;
        private Panel panelUpgrades;
        private Panel panelDilemmas;
        private Panel panelClimateHub;
        private Panel panelAchievements;

        // Header Labels & Controls
        private Label lblPlayerTag;
        private Label lblDate;
        private Label lblBudget;
        private Label lblEnergy;
        private Label lblCarbon;
        private Label lblThreat;
        private Label lblApproval;
        private Panel[] hudCards = new Panel[6];
        private Button btnNextMonth;
        private Button btnSpeed;
        private Button btnPause;
        private Button btnSound;
        private Button btnMainMenu;
        private Button btnSave;
        private Button btnMusic;
        private Button btnInGameGuide;

        // Operations Controls (Tab 1)
        private Panel boxSlider;
        private Panel boxActions;
        private Panel boxContracts;
        private TrackBar tbCoalSlider;
        private Label lblSliderVal;
        private Label lblStatsDetails;
        private Label lblCoalStockBar;
        private Label lblStaminaBar;
        private Label lblTelemetryInfo;
        private List<Button> actionButtonsList = new List<Button>();
        private Label lblActionLogHistory;
        private List<string> recentActionLogs = new List<string>();
        private Label lblContractsInfo;
        private Label lblContractCommitmentDetails;
        private Label lblMarketSpotBourse;
        private Panel lineDivContracts;
        private Label lblNewsTicker;

        // Tab Buttons
        private Button btnTabOps;
        private Button btnTabUpgrades;
        private Button btnTabDilemmas;
        private Button btnTabClimate;
        private Button btnTabAchievements;
        private FlowLayoutPanel flowAchievements;
        private Label lblAchievementsSummary;
        private Timer ambientMusicTimer;
        private int ambientPhraseIndex = 0;
        private string activeTab = "ops";

        // Dilemma Tab Controls (Tab 3)
        private Panel panelActiveDilemmaBox;
        private Panel boxArchive;
        private Label lblDilemmaTitle;
        private Label lblDilemmaSender;
        private Label lblDilemmaDesc;
        private FlowLayoutPanel flowDilemmaChoices;
        private Label lblRiskAnalysis;
        private ListBox listPastDilemmas;

        // Climate Hub Controls (Tab 4)
        private Panel climateLeftBox;
        private Label lblFactTitle;
        private Label lblFactBody;
        private Label lblFactSource;
        private Label lblFactAction;
        private Label lblEcoTracker;
        private Label lblWarmingMeter;
        private Label lblAirQualityBadge;
        private Label lblSinkForestStats;
        private Button btnPrevFact;
        private Button btnNextFact;
        private DoubleBufferedPanel chartPanel;

        // Upgrades flow panel (Tab 2)
        private FlowLayoutPanel flowUpgrades;

        // Notification banner
        private Panel notifPanel;
        private Label lblNotif;
        private Timer notifTimer;
        private int notifTicksLeft = 0;

        public GameForm()
        {
            InitializeFormSettings();
            BuildMenuScreen();
            BuildSettingsScreen();
            BuildHowToPlayScreen();
            BuildNameInputScreen();
            BuildGameScreen();

            LayoutResponsiveControls();
            ShowScreen(GameScreen.MainMenu);
            StartRenderLoop();
        }

        private void InitializeFormSettings()
        {
            this.Text = "Carbon Energy: Maden Müdürü";
            this.ClientSize = new Size(1280, 800);
            this.MinimumSize = new Size(1024, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(14, 16, 20);
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Maximized;
            this.MaximizeBox = true;
            this.Resize += new EventHandler(GameForm_Resize);
        }

        private void GameForm_Resize(object sender, EventArgs e)
        {
            LayoutResponsiveControls();
        }

        private void LayoutResponsiveControls()
        {
            if (this.WindowState == FormWindowState.Minimized) return;

            // 1. Center menu screen items
            if (menuPanel != null && btnMenuPlay != null)
            {
                int btnW = 340;
                int btnH = 46;
                int btnGap = 10;
                int centerX = (menuPanel.ClientSize.Width - btnW) / 2;
                int startY = Math.Max(260, (menuPanel.ClientSize.Height - (btnH + btnGap) * 5) / 2 + 35);

                btnMenuPlay.Location = new Point(centerX, startY);
                if (btnMenuLoad != null) btnMenuLoad.Location = new Point(centerX, startY + (btnH + btnGap) * 1);
                if (btnMenuHowToPlay != null) btnMenuHowToPlay.Location = new Point(centerX, startY + (btnH + btnGap) * 2);
                if (btnMenuSettings != null) btnMenuSettings.Location = new Point(centerX, startY + (btnH + btnGap) * 3);
                if (btnMenuExit != null) btnMenuExit.Location = new Point(centerX, startY + (btnH + btnGap) * 4);
                menuPanel.Invalidate();
            }

            // 2. Settings screen items
            if (settingsPanel != null && settingsCard != null)
            {
                int sCenterX = (settingsPanel.ClientSize.Width - settingsCard.Width) / 2;
                settingsCard.Location = new Point(sCenterX, Math.Max(100, (settingsPanel.ClientSize.Height - settingsCard.Height) / 2 - 20));
                if (lblSettingsTitle != null) lblSettingsTitle.Location = new Point((settingsPanel.ClientSize.Width - lblSettingsTitle.Width) / 2, settingsCard.Top - 80);
                if (lineDecor != null) lineDecor.Location = new Point((settingsPanel.ClientSize.Width - lineDecor.Width) / 2, settingsCard.Top - 28);
                if (btnSettingsBack != null) btnSettingsBack.Location = new Point((settingsPanel.ClientSize.Width - btnSettingsBack.Width) / 2, settingsCard.Bottom + 18);
            }

            // 3. How to play screen items
            if (howToPlayPanel != null && guideCard != null)
            {
                guideCard.Size = new Size(Math.Max(500, howToPlayPanel.ClientSize.Width - 340), Math.Max(400, howToPlayPanel.ClientSize.Height - 125));
                if (txtGuideContent != null) txtGuideContent.Size = new Size(guideCard.Width - 50, guideCard.Height - 105);
            }

            // 4. Name input screen items
            if (namePanel != null && nameCard != null)
            {
                int nCenterX = (namePanel.ClientSize.Width - nameCard.Width) / 2;
                int nCenterY = Math.Max(80, (namePanel.ClientSize.Height - nameCard.Height) / 2 - 30);
                nameCard.Location = new Point(nCenterX, nCenterY);
                if (btnNameBack != null) btnNameBack.Location = new Point(nCenterX, nameCard.Bottom + 20);
            }

            // 5. Responsively adjust canvas height so bottom tab area always has generous vertical space
            if (canvasPanel != null)
            {
                int targetCanvasH = Math.Max(160, Math.Min(220, (this.ClientSize.Height - 114) * 28 / 100));
                if (canvasPanel.Height != targetCanvasH)
                {
                    canvasPanel.Height = targetCanvasH;
                }
            }

            // 6. Refresh chart if active
            if (chartPanel != null && panelClimateHub != null && panelClimateHub.Visible)
            {
                chartPanel.Invalidate();
            }

            // 7. Tab 5: Achievements Responsive Layout
            if (flowAchievements != null && panelAchievements != null && panelAchievements.Visible)
            {
                RefreshAchievementsPanel();
            }
        }

        private void ShowScreen(GameScreen screen)
        {
            currentScreen = screen;

            menuPanel.Visible = (screen == GameScreen.MainMenu);
            settingsPanel.Visible = (screen == GameScreen.Settings);
            howToPlayPanel.Visible = (screen == GameScreen.HowToPlay);
            namePanel.Visible = (screen == GameScreen.NameInput);
            gameContainer.Visible = (screen == GameScreen.Playing || screen == GameScreen.GameOver);

            if (screen == GameScreen.MainMenu)
            {
                menuPanel.BringToFront();
            }
            else if (screen == GameScreen.Settings)
            {
                settingsPanel.BringToFront();
            }
            else if (screen == GameScreen.HowToPlay)
            {
                howToPlayPanel.BringToFront();
            }
            else if (screen == GameScreen.NameInput)
            {
                namePanel.BringToFront();
                txtPlayerName.Focus();
            }
            else if (screen == GameScreen.Playing)
            {
                gameContainer.BringToFront();
            }
        }

        // ==========================================
        // 1. MAIN MENU SCREEN
        // ==========================================
        private void BuildMenuScreen()
        {
            menuPanel = new DoubleBufferedPanel();
            menuPanel.Dock = DockStyle.Fill;
            menuPanel.BackColor = Color.FromArgb(14, 16, 20);
            menuPanel.Paint += new PaintEventHandler(MenuPanel_Paint);

            int btnW = 320;
            int btnH = 46;
            int btnGap = 10;
            int startY = 325;
            int centerX = (1180 - btnW) / 2;

            btnMenuPlay = CreateMenuButton("🎮 YENİ OYUN BAŞLAT", centerX, startY, btnW, btnH, Color.FromArgb(46, 204, 113));
            btnMenuPlay.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                SoundEngine.PlayClick();
                ShowScreen(GameScreen.NameInput);
            });
            menuPanel.Controls.Add(btnMenuPlay);

            string savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "savegame.dat");
            btnMenuLoad = CreateMenuButton("📂 KAYITTAN DEVAM ET", centerX, startY + (btnH + btnGap) * 1, btnW, btnH, Color.FromArgb(52, 152, 219));
            btnMenuLoad.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                if (File.Exists(savePath))
                {
                    GameModel loaded = GameModel.LoadFromFile(savePath);
                    if (loaded != null)
                    {
                        model = loaded;
                        renderer = new WorldRenderer();
                        SoundEngine.PlayCash();
                        ShowScreen(GameScreen.Playing);
                        UpdateHudLabels();
                        UpdateNewsTicker();
                        ShowNotification("📂 Kayıtlı oyun başarıyla yüklendi!", Color.FromArgb(46, 204, 113));
                    }
                    else
                    {
                        MessageBox.Show("Kayıt dosyası okunamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Henüz kaydedilmiş bir oyun bulunmuyor!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            });
            menuPanel.Controls.Add(btnMenuLoad);

            btnMenuHowToPlay = CreateMenuButton("📖 NASIL OYNANIR?", centerX, startY + (btnH + btnGap) * 2, btnW, btnH, Color.FromArgb(241, 196, 15));
            btnMenuHowToPlay.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                SoundEngine.PlayClick();
                ShowScreen(GameScreen.HowToPlay);
            });
            menuPanel.Controls.Add(btnMenuHowToPlay);

            btnMenuSettings = CreateMenuButton("⚙️ AYARLAR", centerX, startY + (btnH + btnGap) * 3, btnW, btnH, Color.FromArgb(142, 68, 173));
            btnMenuSettings.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                SoundEngine.PlayClick();
                ShowScreen(GameScreen.Settings);
            });
            menuPanel.Controls.Add(btnMenuSettings);

            btnMenuExit = CreateMenuButton("🚪 ÇIKIŞ", centerX, startY + (btnH + btnGap) * 4, btnW, btnH, Color.FromArgb(192, 57, 43));
            btnMenuExit.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                Application.Exit();
            });
            menuPanel.Controls.Add(btnMenuExit);

            Label lblVersion = new Label();
            lblVersion.Text = "Carbon Energy v1.0 - 5 Yıllık Tam Görev (2026 - 2031)";
            lblVersion.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblVersion.ForeColor = Color.FromArgb(90, 95, 105);
            lblVersion.AutoSize = true;
            lblVersion.Location = new Point(20, 730);
            menuPanel.Controls.Add(lblVersion);

            Label lblClimateMsg = new Label();
            lblClimateMsg.Text = "Kömür madeninden yeşil enerji üssüne: Zorlaştırılmış ekonomi ve çoktan seçmeli kriz masasıyla gerçekçi simülasyon.";
            lblClimateMsg.Font = new Font("Segoe UI", 9f, FontStyle.Italic);
            lblClimateMsg.ForeColor = Color.FromArgb(46, 204, 113);
            lblClimateMsg.AutoSize = true;
            lblClimateMsg.Location = new Point(250, 730);
            menuPanel.Controls.Add(lblClimateMsg);

            this.Controls.Add(menuPanel);
        }

        private void MenuPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int w = menuPanel.Width;
            int h = menuPanel.Height;

            float phase = menuAnimPhase;
            int r1 = (int)(16 + Math.Sin(phase * 0.3) * 8);
            int g1 = (int)(22 + Math.Sin(phase * 0.4) * 8);
            int b1 = (int)(32 + Math.Sin(phase * 0.5) * 12);
            Color topCol = Color.FromArgb(Math.Max(0, Math.Min(255, r1)), Math.Max(0, Math.Min(255, g1)), Math.Max(0, Math.Min(255, b1)));
            Color botCol = Color.FromArgb(8, 12, 18);

            using (LinearGradientBrush bgBrush = new LinearGradientBrush(new Point(0, 0), new Point(0, h), topCol, botCol))
            {
                g.FillRectangle(bgBrush, 0, 0, w, h);
            }

            Random starRnd = new Random(42);
            for (int i = 0; i < 120; i++)
            {
                float sx = (float)(starRnd.NextDouble() * w);
                float sy = (float)(starRnd.NextDouble() * (h * 0.5));
                float twinkle = (float)(Math.Sin(phase * 2.0 + i * 0.7) * 0.4 + 0.6);
                int alpha = (int)(twinkle * 180);
                using (SolidBrush starBrush = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                {
                    float sr = (float)(starRnd.NextDouble() * 1.5 + 0.5);
                    g.FillEllipse(starBrush, sx, sy, sr, sr);
                }
            }

            using (SolidBrush mtnBrush = new SolidBrush(Color.FromArgb(20, 26, 36)))
            {
                PointF[] mtn = new PointF[] {
                    new PointF(0, h),
                    new PointF(0, 540),
                    new PointF(140, 470),
                    new PointF(310, 510),
                    new PointF(460, 430),
                    new PointF(630, 500),
                    new PointF(790, 410),
                    new PointF(960, 490),
                    new PointF(1110, 450),
                    new PointF(w, 480),
                    new PointF(w, h)
                };
                g.FillPolygon(mtnBrush, mtn);
            }

            float orbPulse = (float)(Math.Sin(phase * 1.5) * 0.3 + 0.7);
            int orbAlpha = (int)(orbPulse * 50);
            using (SolidBrush orbGlow = new SolidBrush(Color.FromArgb(orbAlpha, 46, 204, 113)))
            {
                g.FillEllipse(orbGlow, w / 2 - 200, 75, 400, 400);
            }

            using (Font titleFont = new Font("Segoe UI", 50f, FontStyle.Bold))
            {
                string title = "CARBON ENERGY";
                SizeF titleSize = g.MeasureString(title, titleFont);
                float titleX = (w - titleSize.Width) / 2;
                float titleY = 100;

                using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(40, 46, 204, 113)))
                {
                    g.DrawString(title, titleFont, glowBrush, titleX + 2, titleY + 2);
                }
                using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(46, 204, 113)))
                {
                    g.DrawString(title, titleFont, titleBrush, titleX, titleY);
                }
            }

            using (Font subFont = new Font("Segoe UI", 18f, FontStyle.Regular))
            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(210, 220, 230)))
            {
                string sub = "MADEN MÜDÜRÜ: İKLİM & ENERJİ YÖNETİMİ";
                SizeF subSize = g.MeasureString(sub, subFont);
                g.DrawString(sub, subFont, subBrush, (w - subSize.Width) / 2, 185);
            }

            using (Pen linePen = new Pen(Color.FromArgb(46, 204, 113), 2.5f))
            {
                g.DrawLine(linePen, w / 2 - 180, 230, w / 2 + 180, 230);
            }

            using (Font tagFont = new Font("Segoe UI", 12f, FontStyle.Italic))
            using (SolidBrush tagBrush = new SolidBrush(Color.FromArgb(190, 200, 210)))
            {
                string tag = "Kömür madenini sıfır karbonlu geleceğin enerji parkına dönüştür!";
                SizeF tagSize = g.MeasureString(tag, tagFont);
                g.DrawString(tag, tagFont, tagBrush, (w - tagSize.Width) / 2, 250);
            }
        }

        private Button CreateMenuButton(string text, int x, int y, int w, int h, Color accentColor)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(w, h);
            btn.Location = new Point(x, y);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = accentColor;
            btn.FlatAppearance.BorderSize = 2;
            btn.BackColor = Color.FromArgb(25, 28, 35);
            btn.ForeColor = accentColor;
            btn.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;

            btn.MouseEnter += new EventHandler(delegate(object s, EventArgs ev)
            {
                btn.BackColor = accentColor;
                btn.ForeColor = Color.White;
            });
            btn.MouseLeave += new EventHandler(delegate(object s, EventArgs ev)
            {
                btn.BackColor = Color.FromArgb(25, 28, 35);
                btn.ForeColor = accentColor;
            });

            return btn;
        }

        // ==========================================
        // 2. SETTINGS SCREEN
        // ==========================================
        private void BuildSettingsScreen()
        {
            settingsPanel = new Panel();
            settingsPanel.Dock = DockStyle.Fill;
            settingsPanel.BackColor = Color.FromArgb(18, 20, 26);

            lblSettingsTitle = new Label();
            lblSettingsTitle.Text = "AYARLAR";
            lblSettingsTitle.Font = new Font("Segoe UI", 30f, FontStyle.Bold);
            lblSettingsTitle.ForeColor = Color.FromArgb(52, 152, 219);
            lblSettingsTitle.AutoSize = true;
            lblSettingsTitle.Location = new Point(480, 40);
            settingsPanel.Controls.Add(lblSettingsTitle);

            lineDecor = new Panel();
            lineDecor.Size = new Size(220, 2);
            lineDecor.Location = new Point(480, 92);
            lineDecor.BackColor = Color.FromArgb(52, 152, 219);
            settingsPanel.Controls.Add(lineDecor);

            settingsCard = new Panel();
            settingsCard.Size = new Size(540, 430);
            settingsCard.Location = new Point(320, 120);
            settingsCard.BackColor = Color.FromArgb(28, 31, 38);
            settingsPanel.Controls.Add(settingsCard);

            int yPos = 25;
            int labelX = 30;
            int controlX = 250;

            Label lblSoundTitle = new Label();
            lblSoundTitle.Text = "SES VE AKUSTİK AYARLARI";
            lblSoundTitle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblSoundTitle.ForeColor = Color.FromArgb(46, 204, 113);
            lblSoundTitle.Location = new Point(labelX, yPos);
            lblSoundTitle.AutoSize = true;
            settingsCard.Controls.Add(lblSoundTitle);

            yPos += 35;

            Label lblSound = new Label();
            lblSound.Text = "Ses Efektleri:";
            lblSound.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            lblSound.ForeColor = Color.FromArgb(200, 205, 215);
            lblSound.Location = new Point(labelX, yPos + 3);
            lblSound.AutoSize = true;
            settingsCard.Controls.Add(lblSound);

            chkSoundEnabled = new CheckBox();
            chkSoundEnabled.Text = "Açık";
            chkSoundEnabled.Checked = SoundEngine.SoundEnabled;
            chkSoundEnabled.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            chkSoundEnabled.ForeColor = Color.White;
            chkSoundEnabled.Location = new Point(controlX, yPos);
            chkSoundEnabled.AutoSize = true;
            chkSoundEnabled.CheckedChanged += new EventHandler(delegate(object s, EventArgs ev)
            {
                SoundEngine.SoundEnabled = chkSoundEnabled.Checked;
                chkSoundEnabled.Text = chkSoundEnabled.Checked ? "Açık" : "Kapalı";
            });
            settingsCard.Controls.Add(chkSoundEnabled);

            yPos += 40;

            Label lblVol = new Label();
            lblVol.Text = "Ses Seviyesi:";
            lblVol.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            lblVol.ForeColor = Color.FromArgb(200, 205, 215);
            lblVol.Location = new Point(labelX, yPos + 5);
            lblVol.AutoSize = true;
            settingsCard.Controls.Add(lblVol);

            tbVolume = new TrackBar();
            tbVolume.Minimum = 0;
            tbVolume.Maximum = 100;
            tbVolume.Value = (int)(SoundEngine.Volume * 100);
            tbVolume.TickFrequency = 10;
            tbVolume.Location = new Point(controlX, yPos);
            tbVolume.Size = new Size(180, 30);
            tbVolume.ValueChanged += new EventHandler(delegate(object s, EventArgs ev)
            {
                SoundEngine.Volume = tbVolume.Value / 100f;
                lblVolumeVal.Text = string.Format("%{0}", tbVolume.Value);
            });
            settingsCard.Controls.Add(tbVolume);

            lblVolumeVal = new Label();
            lblVolumeVal.Text = string.Format("%{0}", tbVolume.Value);
            lblVolumeVal.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            lblVolumeVal.ForeColor = Color.FromArgb(241, 196, 15);
            lblVolumeVal.Location = new Point(440, yPos + 5);
            lblVolumeVal.AutoSize = true;
            settingsCard.Controls.Add(lblVolumeVal);

            yPos += 55;

            Label lblDiffTitle = new Label();
            lblDiffTitle.Text = "ZORLUK VE EKONOMİ SEVİYESİ";
            lblDiffTitle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblDiffTitle.ForeColor = Color.FromArgb(46, 204, 113);
            lblDiffTitle.Location = new Point(labelX, yPos);
            lblDiffTitle.AutoSize = true;
            settingsCard.Controls.Add(lblDiffTitle);

            yPos += 35;

            Label lblDiff = new Label();
            lblDiff.Text = "Zorluk Derecesi:";
            lblDiff.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            lblDiff.ForeColor = Color.FromArgb(200, 205, 215);
            lblDiff.Location = new Point(labelX, yPos + 3);
            lblDiff.AutoSize = true;
            settingsCard.Controls.Add(lblDiff);

            cmbDifficulty = new ComboBox();
            cmbDifficulty.Items.AddRange(new object[] { "Kolay (Başlangıç ₺100,000)", "Normal (Başlangıç ₺65,000 + Karbon Vergisi)", "Zorlu Kriz (Başlangıç ₺40,000 + Ağır Vergi)" });
            cmbDifficulty.SelectedIndex = 1;
            cmbDifficulty.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDifficulty.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            cmbDifficulty.Location = new Point(controlX, yPos);
            cmbDifficulty.Size = new Size(260, 28);
            settingsCard.Controls.Add(cmbDifficulty);

            yPos += 42;

            Label lblSpeedSetting = new Label();
            lblSpeedSetting.Text = "Zaman İlerleme Hızı:";
            lblSpeedSetting.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            lblSpeedSetting.ForeColor = Color.FromArgb(200, 205, 215);
            lblSpeedSetting.Location = new Point(labelX, yPos + 3);
            lblSpeedSetting.AutoSize = true;
            settingsCard.Controls.Add(lblSpeedSetting);

            cmbGameSpeed = new ComboBox();
            cmbGameSpeed.Items.AddRange(new object[] { "Yavaş (4 sn / ay)", "Normal (3 sn / ay)", "Hızlı (2 sn / ay)" });
            cmbGameSpeed.SelectedIndex = 1;
            cmbGameSpeed.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbGameSpeed.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            cmbGameSpeed.Location = new Point(controlX, yPos);
            cmbGameSpeed.Size = new Size(260, 28);
            settingsCard.Controls.Add(cmbGameSpeed);

            yPos += 55;

            Label lblInfoTitle = new Label();
            lblInfoTitle.Text = "HAKKINDA";
            lblInfoTitle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblInfoTitle.ForeColor = Color.FromArgb(46, 204, 113);
            lblInfoTitle.Location = new Point(labelX, yPos);
            lblInfoTitle.AutoSize = true;
            settingsCard.Controls.Add(lblInfoTitle);

            yPos += 30;

            Label lblAbout = new Label();
            lblAbout.Text = "Carbon Energy, karbon vergisi, sözleşme teslimatı ve çok seçenekli\nkriz olaylarıyla zenginleştirilmiş bağımsız bir strateji simülasyonudur.";
            lblAbout.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblAbout.ForeColor = Color.FromArgb(170, 175, 185);
            lblAbout.Location = new Point(labelX, yPos);
            lblAbout.Size = new Size(480, 55);
            settingsCard.Controls.Add(lblAbout);

            btnSettingsBack = new Button();
            btnSettingsBack.Text = "GERİ DÖN";
            btnSettingsBack.Size = new Size(200, 46);
            btnSettingsBack.Location = new Point(490, 580);
            btnSettingsBack.FlatStyle = FlatStyle.Flat;
            btnSettingsBack.FlatAppearance.BorderColor = Color.FromArgb(150, 155, 165);
            btnSettingsBack.BackColor = Color.FromArgb(35, 38, 45);
            btnSettingsBack.ForeColor = Color.White;
            btnSettingsBack.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            btnSettingsBack.Cursor = Cursors.Hand;
            btnSettingsBack.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                SoundEngine.PlayClick();
                ShowScreen(GameScreen.MainMenu);
            });
            settingsPanel.Controls.Add(btnSettingsBack);

            this.Controls.Add(settingsPanel);
        }

        // ==========================================
        // 2.5 HOW TO PLAY / DIRECTOR'S GUIDE SCREEN
        // ==========================================
        private void BuildHowToPlayScreen()
        {
            howToPlayPanel = new Panel();
            howToPlayPanel.Dock = DockStyle.Fill;
            howToPlayPanel.BackColor = Color.FromArgb(14, 18, 24);

            // Top Header Panel
            Panel topHeader = new Panel();
            topHeader.Dock = DockStyle.Top;
            topHeader.Height = 85;
            topHeader.BackColor = Color.FromArgb(20, 24, 32);

            Label lblTitle = new Label();
            lblTitle.Text = "📖 MADEN MÜDÜRÜ GÖREV REHBERİ — NASIL OYNANIR?";
            lblTitle.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(46, 204, 113);
            lblTitle.Location = new Point(25, 15);
            lblTitle.AutoSize = true;
            topHeader.Controls.Add(lblTitle);

            Label lblSub = new Label();
            lblSub.Text = "5 Yıllık (60 Ay) İklim Krizi, Kömür Madenciliği ve Yeşil Enerji Dönüşümü El Kitabı";
            lblSub.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            lblSub.ForeColor = Color.FromArgb(150, 160, 175);
            lblSub.Location = new Point(27, 46);
            lblSub.AutoSize = true;
            topHeader.Controls.Add(lblSub);

            btnHowToPlayBack = new Button();
            btnHowToPlayBack.Text = "⬅️ GERİ DÖN";
            btnHowToPlayBack.Size = new Size(160, 42);
            btnHowToPlayBack.Location = new Point(980, 20);
            btnHowToPlayBack.FlatStyle = FlatStyle.Flat;
            btnHowToPlayBack.FlatAppearance.BorderColor = Color.FromArgb(150, 155, 165);
            btnHowToPlayBack.BackColor = Color.FromArgb(35, 38, 45);
            btnHowToPlayBack.ForeColor = Color.White;
            btnHowToPlayBack.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnHowToPlayBack.Cursor = Cursors.Hand;
            btnHowToPlayBack.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                SoundEngine.PlayClick();
                if (model != null && !model.GameOver)
                {
                    ShowScreen(GameScreen.Playing);
                }
                else
                {
                    ShowScreen(GameScreen.MainMenu);
                }
            });
            topHeader.Controls.Add(btnHowToPlayBack);

            howToPlayPanel.Controls.Add(topHeader);

            // Left Sidebar for Tabs
            Panel leftNav = new Panel();
            leftNav.Location = new Point(25, 105);
            leftNav.Size = new Size(270, 560);
            leftNav.BackColor = Color.FromArgb(20, 24, 32);

            Label lblCategories = new Label();
            lblCategories.Text = "REHBER KONULARI";
            lblCategories.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblCategories.ForeColor = Color.FromArgb(120, 130, 145);
            lblCategories.Location = new Point(15, 15);
            lblCategories.AutoSize = true;
            leftNav.Controls.Add(lblCategories);

            string[] tabNames = new string[] {
                "🎯 1. Amaç & 5 Yıllık Hedef",
                "💰 2. Para & Zor Ekonomi",
                "🖱️ 3. Canlı Saha & Tıklama",
                "⚖️ 4. Kriz Masası & Seçenekler",
                "🏆 5. Zaferler & Taktikler"
            };

            guideCategoryBtns = new Button[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++)
            {
                int idx = i;
                Button btn = new Button();
                btn.Text = tabNames[i];
                btn.Size = new Size(240, 52);
                btn.Location = new Point(15, 45 + i * 58);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = (i == 0) ? Color.FromArgb(46, 204, 113) : Color.FromArgb(40, 48, 60);
                btn.BackColor = (i == 0) ? Color.FromArgb(32, 55, 45) : Color.FromArgb(25, 30, 40);
                btn.ForeColor = (i == 0) ? Color.FromArgb(46, 204, 113) : Color.FromArgb(200, 205, 215);
                btn.Font = new Font("Segoe UI", 9.2f, FontStyle.Bold);
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.Padding = new Padding(10, 0, 0, 0);
                btn.Cursor = Cursors.Hand;
                btn.Click += new EventHandler(delegate(object s, EventArgs ev)
                {
                    SoundEngine.PlayClick();
                    SelectGuideTab(idx);
                });
                guideCategoryBtns[i] = btn;
                leftNav.Controls.Add(btn);
            }

            // Quick play button on left nav bottom
            btnHowToPlayStart = new Button();
            btnHowToPlayStart.Text = "🎮 HEMEN OYNA";
            btnHowToPlayStart.Size = new Size(240, 50);
            btnHowToPlayStart.Location = new Point(15, 490);
            btnHowToPlayStart.FlatStyle = FlatStyle.Flat;
            btnHowToPlayStart.FlatAppearance.BorderSize = 0;
            btnHowToPlayStart.BackColor = Color.FromArgb(46, 204, 113);
            btnHowToPlayStart.ForeColor = Color.Black;
            btnHowToPlayStart.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnHowToPlayStart.Cursor = Cursors.Hand;
            btnHowToPlayStart.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                SoundEngine.PlayClick();
                ShowScreen(GameScreen.NameInput);
            });
            leftNav.Controls.Add(btnHowToPlayStart);

            howToPlayPanel.Controls.Add(leftNav);

            // Right Main Card
            guideCard = new Panel();
            guideCard.Location = new Point(315, 105);
            guideCard.Size = new Size(835, 560);
            guideCard.BackColor = Color.FromArgb(20, 24, 32);

            lblGuideSectionTitle = new Label();
            lblGuideSectionTitle.Font = new Font("Segoe UI", 13.5f, FontStyle.Bold);
            lblGuideSectionTitle.ForeColor = Color.FromArgb(46, 204, 113);
            lblGuideSectionTitle.Location = new Point(25, 18);
            lblGuideSectionTitle.AutoSize = true;
            guideCard.Controls.Add(lblGuideSectionTitle);

            lblGuideSectionBadge = new Label();
            lblGuideSectionBadge.Font = new Font("Segoe UI", 9f, FontStyle.Italic);
            lblGuideSectionBadge.ForeColor = Color.FromArgb(241, 196, 15);
            lblGuideSectionBadge.Location = new Point(26, 46);
            lblGuideSectionBadge.AutoSize = true;
            guideCard.Controls.Add(lblGuideSectionBadge);

            txtGuideContent = new TextBox();
            txtGuideContent.Location = new Point(25, 80);
            txtGuideContent.Size = new Size(785, 455);
            txtGuideContent.Multiline = true;
            txtGuideContent.ReadOnly = true;
            txtGuideContent.ScrollBars = ScrollBars.Vertical;
            txtGuideContent.BorderStyle = BorderStyle.None;
            txtGuideContent.BackColor = Color.FromArgb(20, 24, 32);
            txtGuideContent.ForeColor = Color.FromArgb(220, 228, 238);
            txtGuideContent.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            guideCard.Controls.Add(txtGuideContent);

            howToPlayPanel.Controls.Add(guideCard);

            this.Controls.Add(howToPlayPanel);

            SelectGuideTab(0);
        }

        private void SelectGuideTab(int index)
        {
            currentGuideTab = index;
            for (int i = 0; i < guideCategoryBtns.Length; i++)
            {
                if (i == index)
                {
                    guideCategoryBtns[i].FlatAppearance.BorderColor = Color.FromArgb(46, 204, 113);
                    guideCategoryBtns[i].BackColor = Color.FromArgb(32, 55, 45);
                    guideCategoryBtns[i].ForeColor = Color.FromArgb(46, 204, 113);
                }
                else
                {
                    guideCategoryBtns[i].FlatAppearance.BorderColor = Color.FromArgb(40, 48, 60);
                    guideCategoryBtns[i].BackColor = Color.FromArgb(25, 30, 40);
                    guideCategoryBtns[i].ForeColor = Color.FromArgb(200, 205, 215);
                }
            }

            if (index == 0)
            {
                lblGuideSectionTitle.Text = "🎯 GENEL AMAÇ & 5 YILLIK MÜDÜRLÜK GÖREVİ (2026 - 2031)";
                lblGuideSectionBadge.Text = "Kritik Eşik: +1.5°C İklim Sınırı | 60 Ay (5 Yıl) | Sıfır Karbon Dönüşümü";
                txtGuideContent.Text =
                    "■ HİKAYE VE ROLÜNÜZ:\r\n" +
                    "Yıl 2026. Ülkenin en köklü ancak en çok karbon salan enerji tesislerinden 'Karbon-9 Maden İşletmesi'ne yeni Maden Müdürü olarak atandınız. Önünüzde tam 60 Ay (5 Yıl) sürecek zorlu bir görev süresi bulunuyor.\r\n\r\n" +
                    "■ TEMEL HEDEFLERİNİZ:\r\n" +
                    "1. Kesintisiz Enerji: Şehrin ve sanayinin elektrik ihtiyacını bir an olsun aksatmadan karşılayın.\r\n" +
                    "2. Yeşil Dönüşüm: Kömür yakımını aşamalı olarak azaltıp rüzgar, güneş, batarya ve jeotermal payını %70'in üzerine çıkarın.\r\n" +
                    "3. Karbonu Sıfırlamak: Bacalardan çıkan CO₂ salımını filtreler, ağaçlandırma ve yenilenebilir enerjiyle net sıfıra indirin.\r\n" +
                    "4. İklim Eşiği: Küresel sıcaklık artışını kritik eşik olan +1.5°C altında tutun!\r\n\r\n" +
                    "■ OYUNU KAYBETME (ÇÖKÜŞ) KOŞULLARI:\r\n" +
                    "❌ İFLAS: Şirket kasası -₺50.000 altına düşerse iflas bayrağı çekilir ve göreviniz sonlandırılır.\r\n" +
                    "❌ EKOLOJİK ÇÖKÜŞ: Küresel ısınma artışı +2.0°C seviyesine ulaşırsa veya zehirli dumanlar vadiyi kaplarsa Bakanlık tesisi mühürler!\r\n" +
                    "❌ HALK İSYANI: Ağır kirlilik, işçi ihmalleri ve patlamalar nedeniyle halk desteği %10 altına inerse protestolarla görevden alınırsınız!\r\n\r\n" +
                    "💡 İPUCU: 60 ayı başarıyla tamamladığınızda yeşil enerji oranınıza ve çevre başarınıza göre 'Platin', 'Altın', 'Gümüş' veya 'Bronz' Maden Müdürü unvanı kazanırsınız!";
            }
            else if (index == 1)
            {
                lblGuideSectionTitle.Text = "💰 ZORLAŞTIRILMIŞ EKONOMİ & MANUEL PARA KAZANMA SİSTEMİ";
                lblGuideSectionBadge.Text = "Stok Yönetimi | İşçi Kondisyonu | Elektrik Sözleşmeleri | Spot Enerji Borsası | Karbon Vergisi";
                txtGuideContent.Text =
                    "■ ARTIK OTOMATİK GELİR YOK:\r\n" +
                    "Yeni sistemde para kazanmak gerçek bir yönetim becerisi ve planlama gerektirir.\r\n\r\n" +
                    "■ 1. HAM KÖMÜR REZERVİ & MANUEL VARDİYA:\r\n" +
                    "• Termik santral havadan çalışmaz; ambardaki kömür stokunu tüketir.\r\n" +
                    "• Kömür stoğu biterse santral durur, sözleşmeler aksar ve ağır cezalar ödersiniz.\r\n" +
                    "• Stoğu artırmak için haritadaki maden asansörüne tıklayın veya 'Ekstra Kömür Vardiyası' butonunu kullanın.\r\n\r\n" +
                    "■ 2. İŞÇİ KONDİSYONU (STAMINA) & RİSK:\r\n" +
                    "• Her vardiya işçilerin kondisyonunu düşürür. Kondisyon %30 altına düştüğünde iş kazası ve grev riski tırmanır.\r\n" +
                    "• 'İşçi Primi & Sağlık Desteği' (₺4,200) vererek işçi moralini ve kondisyonunu toparlayın.\r\n\r\n" +
                    "■ 3. UZUN VADELİ ELEKTRİK SÖZLEŞMELERİ:\r\n" +
                    "• Belediye Şebekesi Sözleşmesi: Aylık 450 MWh elektrik talep eder.\r\n" +
                    "• Ağır Sanayi Sözleşmesi: Aylık 300 MWh elektrik talep eder.\r\n" +
                    "• Bu iki sözleşmeyi karşıladığınızda kasanıza garantili gelir girer.\r\n\r\n" +
                    "■ 4. SPOT ENERJİ BORSASI (SERBEST SATIŞ):\r\n" +
                    "• Sözleşmelerden arta kalan fazla elektriğiniz varsa, 'Borsaya Sat' butonuyla anlık piyasa fiyatından nakit paraya dönüştürebilirsiniz!\r\n\r\n" +
                    "■ 5. NET KARBON VERGİSİ (₺20 / TON):\r\n" +
                    "• Atmosfere saldığınız her net ton CO₂ için devlet ₺20 ceza vergisi keser.\r\n" +
                    "• Karbon salımını düşürmezseniz vergi yükü her ay kasanızı tüketir ve sizi iflasa sürükler!";
            }
            else if (index == 2)
            {
                lblGuideSectionTitle.Text = "🖱️ CANLI SAHA ETKİLEŞİMİ & HARİTAYA TIKLAMA REHBERİ";
                lblGuideSectionBadge.Text = "Görsel Dünyadaki Yapılara Doğrudan Tıklayarak Sahayı Yönetin!";
                txtGuideContent.Text =
                    "■ DOKUNARAK YÖNETİLEN ENERJİ VADİSİ:\r\n" +
                    "Oyun ekranının üstündeki animasyonlu sanayi vadisindeki nesnelere farenizle doğrudan tıklayabilirsiniz:\r\n\r\n" +
                    "⛏️ MADEN ASANSÖRÜ & DERİN TÜNEL:\r\n" +
                    "• Tıkladığınızda ocaktan kömür vagonu çıkar (+120 ton stok, +₺4,500 sıcak gelir, işçi yorulur).\r\n\r\n" +
                    "🏭 TERMİK SANTRAL BACALARI:\r\n" +
                    "• Tıkladığınızda bacalarda anlık kimyasal yıkama başlatılır (-45 ton birikmiş kurum ve CO₂ temizliği).\r\n\r\n" +
                    "🌲 YEŞİL DAĞ YAMAÇLARI:\r\n" +
                    "• Yamaçlara tıkladığınızda hızlı fidan dikimi yapılır (+15 ton doğal karbon yutağı, +%4 halk desteği).\r\n\r\n" +
                    "⚡ RÜZGAR TÜRBİNLERİ & GÜNEŞ TARLASI:\r\n" +
                    "• Temiz santrallere tıkladığınızda verim optimizasyonu yapılır (+40 MWh anlık temiz enerji).\r\n\r\n" +
                    "💨 GÖKYÜZÜNE YÜKSELEN SİYAH DUMANLAR:\r\n" +
                    "• Bacalardan çıkan siyah karbon bulutlarına fareyle tıklayarak havayı partikül bazında arıtın ve eko-teşvik kazanın!\r\n\r\n" +
                    "■ ZAMAN VE HIZ KONTROLLERİ:\r\n" +
                    "• [⏭️ AY ATLA]: Beklemeden anında bir sonraki aya geçer.\r\n" +
                    "• [▶ 1x] / [⏩ 2x]: Zamanın akış hızını kontrol eder.\r\n" +
                    "• [⏸ Duraklat]: Karar verirken zamanı durdurur.";
            }
            else if (index == 3)
            {
                lblGuideSectionTitle.Text = "⚖️ KRİZ MASASI & ÇOKTAN SEÇMELİ TAKTİKSEL KARARLAR";
                lblGuideSectionBadge.Text = "3-4 Farklı Karar Opsiyonu | Gerçekçi Ahlaki ve Ekonomik İkilemler";
                txtGuideContent.Text =
                    "■ HER SEÇİMİN BİR BEDELİ VARDIR:\r\n" +
                    "Her birkaç ayda bir ekrana tam boyutlu Kriz Masası penceresi açılır. Karşılaştığınız krizlerde artık basit doğru/yanlış cevaplar yoktur; her kararın farklı departmanlara faturası vardır:\r\n\r\n" +
                    "■ ÖRNEK KRİZLER VE SEÇENEK STRATEJİLERİ:\r\n\r\n" +
                    "📢 Genç İklim Aktivistleri Nizamiyeyi Kuşattı:\r\n" +
                    "• Polis çağırıp dağıt: Üretim aksamaz ancak halk desteği çöker.\r\n" +
                    "• Çay ikram edip yeşil planı sun: Küçük bütçe harcar, halk desteği kazandırır.\r\n" +
                    "• Çatıya ilk güneş panelini birlikte kur: Maliyetlidir ama kömür bağımlılığını kırar ve müthiş itibar getirir!\r\n\r\n" +
                    "⚠️ Galeride Metan Gazı Alarmı:\r\n" +
                    "• Ocağı kapatıp drenaj kur: ₺12.000 harcar ve üretimi durdurur; ancak işçi can güvenliğini %100 sağlar.\r\n" +
                    "• Emişli fanlarla üretime devam et: Ucuzdur ama patlama riski taşır.\r\n" +
                    "• Galeriyi betonla mühürle: Kömürden kalıcı çıkış yapar, işçileri güneş tarlasına transfer eder.\r\n\r\n" +
                    "📜 GEÇMİŞ KARARLAR ARŞİVİ:\r\n" +
                    "• Kriz Masası sekmesindeki 'Geçmiş Kararlar Arşivi' panosundan görev süreniz boyunca aldığınız her kararın detaylarını ve sonuçlarını dilediğiniz zaman inceleyebilirsiniz.";
            }
            else if (index == 4)
            {
                lblGuideSectionTitle.Text = "🏆 ZAFER DERECELERİ & USTA MÜDÜR TAKTİKLERİ";
                lblGuideSectionBadge.Text = "Platin Kupa | Finansal Denge | Dönüşümün Altın Formülü";
                txtGuideContent.Text =
                    "■ 60. AY SONUNDAKİ ZAFER SEVİYELERİ:\r\n\r\n" +
                    "🌟 PLATİN MÜDÜR (Sıfır Karbon Efsanesi):\r\n" +
                    "• Yenilenebilir Enerji Payı > %80\r\n" +
                    "• Net Karbon Salımı <= 0 (Karbon Negatif)\r\n" +
                    "• Kasa Bakiyesi >= ₺100.000\r\n" +
                    "• Halk Güveni >= %70\r\n\r\n" +
                    "🥇 ALTIN MÜDÜR (Yeşil Lider):\r\n" +
                    "• Yenilenebilir Payı > %65, Net Karbon <= 100 Ton, Bütçe Pozitif.\r\n\r\n" +
                    "🥈 GÜMÜŞ MÜDÜR (Dengeli Dönüşüm):\r\n" +
                    "• Yenilenebilir Payı > %45, Kömür büyük oranda kontrol altında.\r\n\r\n" +
                    "🥉 BRONZ MÜDÜR (Hayatta Kalan):\r\n" +
                    "• 60 ayı batmadan tamamlayan ancak temiz enerji payı düşük kalan işletme.\r\n\r\n" +
                    "■ MÜDÜR İÇİN ALTIN TAKTİKLER:\r\n" +
                    "1. İlk 6 Ay: Kömür sözleşmelerini tam teslim ederek sıcak para biriktirin.\r\n" +
                    "2. Karbon Vergisini Kırın: İlk biriktirdiğiniz parayla 'Elektrostatik Filtre' ve 'Ağaçlandırma' yapın. Aylık vergi faturanız düşünce kâr marjınız katlanır.\r\n" +
                    "3. Üretim Slider Ayarı: Rüzgar ve Güneş santralleri kurdukça üretim kaydıracından kömür payını kademe kademe kısın.\r\n" +
                    "4. İşçi Grevini Önleyin: İşçi kondisyonu %35'in altına düştüğünde asla ertelemeden prim dağıtın.\r\n" +
                    "5. Fazla Enerjiyi Satın: Temiz santraller şehir ihtiyacını aştığında arta kalan elektriği spot piyasada anında nakde çevirin!";
            }
        }

        // ==========================================
        // 3. NAME INPUT SCREEN
        // ==========================================
        private void BuildNameInputScreen()
        {
            namePanel = new Panel();
            namePanel.Dock = DockStyle.Fill;
            namePanel.BackColor = Color.FromArgb(14, 18, 24);

            nameCard = new Panel();
            nameCard.Size = new Size(540, 380);
            nameCard.Location = new Point(320, 170);
            nameCard.BackColor = Color.FromArgb(26, 29, 36);
            namePanel.Controls.Add(nameCard);

            Label lblIcon = new Label();
            lblIcon.Text = "CARBON ENERGY";
            lblIcon.Font = new Font("Segoe UI", 22f, FontStyle.Bold);
            lblIcon.ForeColor = Color.FromArgb(46, 204, 113);
            lblIcon.Location = new Point(125, 20);
            lblIcon.AutoSize = true;
            nameCard.Controls.Add(lblIcon);

            Panel lineDecor2 = new Panel();
            lineDecor2.Size = new Size(320, 2);
            lineDecor2.Location = new Point(110, 65);
            lineDecor2.BackColor = Color.FromArgb(46, 204, 113);
            nameCard.Controls.Add(lineDecor2);

            Label lblBrief = new Label();
            lblBrief.Text = "Yıl 2026. Karbon-9 Maden İşletmesi'nin başına atandınız.\n5 Yıl (60 Ay) içinde karbon vergileri, sözleşmeler ve zorlu krizler\naltında madeni sıfır karbonlu bir tesise dönüştürmelisiniz!";
            lblBrief.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            lblBrief.ForeColor = Color.FromArgb(200, 205, 215);
            lblBrief.Location = new Point(35, 80);
            lblBrief.Size = new Size(470, 80);
            nameCard.Controls.Add(lblBrief);

            lblNamePrompt = new Label();
            lblNamePrompt.Text = "Maden Müdürünün Adı ve Soyadı:";
            lblNamePrompt.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblNamePrompt.ForeColor = Color.FromArgb(241, 196, 15);
            lblNamePrompt.Location = new Point(35, 175);
            lblNamePrompt.AutoSize = true;
            nameCard.Controls.Add(lblNamePrompt);

            txtPlayerName = new TextBox();
            txtPlayerName.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
            txtPlayerName.Text = "Ali Kaya";
            txtPlayerName.BackColor = Color.FromArgb(38, 42, 52);
            txtPlayerName.ForeColor = Color.White;
            txtPlayerName.BorderStyle = BorderStyle.FixedSingle;
            txtPlayerName.Location = new Point(35, 210);
            txtPlayerName.Size = new Size(470, 34);
            txtPlayerName.MaxLength = 28;
            txtPlayerName.TextAlign = HorizontalAlignment.Center;
            nameCard.Controls.Add(txtPlayerName);

            btnStartGame = new Button();
            btnStartGame.Text = "GÖREVE BAŞLA (5 YIL)";
            btnStartGame.Size = new Size(320, 52);
            btnStartGame.Location = new Point(110, 275);
            btnStartGame.FlatStyle = FlatStyle.Flat;
            btnStartGame.FlatAppearance.BorderColor = Color.FromArgb(46, 204, 113);
            btnStartGame.FlatAppearance.BorderSize = 2;
            btnStartGame.BackColor = Color.FromArgb(46, 204, 113);
            btnStartGame.ForeColor = Color.Black;
            btnStartGame.Font = new Font("Segoe UI", 15f, FontStyle.Bold);
            btnStartGame.Cursor = Cursors.Hand;
            btnStartGame.Click += new EventHandler(BtnStartGame_Click);

            txtPlayerName.KeyDown += new KeyEventHandler(delegate(object s, KeyEventArgs ev)
            {
                if (ev.KeyCode == Keys.Enter)
                {
                    ev.SuppressKeyPress = true;
                    BtnStartGame_Click(s, ev);
                }
            });

            nameCard.Controls.Add(btnStartGame);

            btnNameBack = new Button();
            btnNameBack.Text = "< Ana Menü";
            btnNameBack.Size = new Size(110, 36);
            btnNameBack.Location = new Point(320, 570);
            btnNameBack.FlatStyle = FlatStyle.Flat;
            btnNameBack.BackColor = Color.FromArgb(35, 38, 45);
            btnNameBack.ForeColor = Color.FromArgb(160, 165, 175);
            btnNameBack.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            btnNameBack.Cursor = Cursors.Hand;
            btnNameBack.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                SoundEngine.PlayClick();
                ShowScreen(GameScreen.MainMenu);
            });
            namePanel.Controls.Add(btnNameBack);

            this.Controls.Add(namePanel);
        }

        private void BtnStartGame_Click(object sender, EventArgs e)
        {
            string pName = txtPlayerName.Text.Trim();
            if (pName.Length == 0) pName = "Ali Kaya";

            int diffIdx = cmbDifficulty.SelectedIndex;
            int speedIdx = cmbGameSpeed.SelectedIndex;

            model = new GameModel();
            renderer = new WorldRenderer();
            model.PlayerName = pName;
            model.Difficulty = diffIdx;

            if (diffIdx == 0)
            {
                model.Budget = 100000;
                model.CarbonTaxRatePerTon = 10.0;
                model.PublicApproval = 75;
            }
            else if (diffIdx == 2)
            {
                model.Budget = 40000;
                model.CarbonTaxRatePerTon = 28.0;
                model.PublicApproval = 55;
                model.BaseOperationalExpense = 14000;
            }

            model.RecalculateStats();

            if (speedIdx == 0) monthTimer.Interval = 4000;
            else if (speedIdx == 1) monthTimer.Interval = 3000;
            else monthTimer.Interval = 2000;

            model.GameSpeed = 1;
            model.IsPaused = false;

            lblPlayerTag.Text = string.Format("Müdür: {0}", model.PlayerName);
            tbCoalSlider.Value = 100;
            UpdateHudLabels();
            SwitchTab("ops");

            monthTimer.Start();
            SoundEngine.PlayUpgrade();
            model.AddEvent("Göreve başladı: " + model.PlayerName);
            ShowScreen(GameScreen.Playing);
        }

        // ==========================================
        // 4. GAME SCREEN (HUD + CANVAS + TABS)
        // ==========================================
        private void BuildGameScreen()
        {
            gameContainer = new Panel();
            gameContainer.Dock = DockStyle.Fill;
            gameContainer.BackColor = Color.FromArgb(24, 26, 30);

            // 1. TOP HEADER HUD
            headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 72;
            headerPanel.BackColor = Color.FromArgb(16, 18, 22);
            headerPanel.Padding = new Padding(8, 4, 8, 4);
            BuildHeaderControls();
            gameContainer.Controls.Add(headerPanel);

            // 2. CANVAS
            canvasPanel = new DoubleBufferedPanel();
            canvasPanel.Dock = DockStyle.Top;
            canvasPanel.Height = 245;
            canvasPanel.BackColor = Color.Black;
            canvasPanel.Cursor = Cursors.Hand;
            canvasPanel.Paint += new PaintEventHandler(CanvasPanel_Paint);
            canvasPanel.MouseClick += new MouseEventHandler(CanvasPanel_MouseClick);
            gameContainer.Controls.Add(canvasPanel);

            // 3. TAB NAVIGATION
            tabNavPanel = new Panel();
            tabNavPanel.Dock = DockStyle.Top;
            tabNavPanel.Height = 42;
            tabNavPanel.BackColor = Color.FromArgb(22, 25, 30);
            BuildTabBarControls();
            gameContainer.Controls.Add(tabNavPanel);

            // 4. NOTIFICATION BAR
            notifPanel = new Panel();
            notifPanel.Dock = DockStyle.Top;
            notifPanel.Height = 0;
            notifPanel.BackColor = Color.FromArgb(241, 196, 15);
            notifPanel.Visible = false;

            lblNotif = new Label();
            lblNotif.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblNotif.ForeColor = Color.Black;
            lblNotif.Location = new Point(12, 5);
            lblNotif.AutoSize = true;
            notifPanel.Controls.Add(lblNotif);
            gameContainer.Controls.Add(notifPanel);

            notifTimer = new Timer();
            notifTimer.Interval = 100;
            notifTimer.Tick += new EventHandler(delegate(object s, EventArgs ev)
            {
                notifTicksLeft--;
                if (notifTicksLeft <= 0)
                {
                    notifPanel.Visible = false;
                    notifPanel.Height = 0;
                    notifTimer.Stop();
                }
            });

            // 5. CONTENT AREA
            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BackColor = Color.FromArgb(24, 26, 30);
            contentPanel.Padding = new Padding(8);

            BuildOperationsTab();
            BuildUpgradesTab();
            BuildDilemmasTab();
            BuildClimateHubTab();
            BuildAchievementsTab();

            gameContainer.Controls.Add(contentPanel);
            this.Controls.Add(gameContainer);
        }

        private void ShowNotification(string message, Color bgColor)
        {
            lblNotif.Text = message;
            notifPanel.BackColor = bgColor;
            notifPanel.Height = 28;
            notifPanel.Visible = true;
            notifTicksLeft = 40;
            notifTimer.Start();
        }

        private void BuildHeaderControls()
        {
            Panel pnlHeaderLeft = new Panel();
            pnlHeaderLeft.Dock = DockStyle.Left;
            pnlHeaderLeft.Width = 150;
            pnlHeaderLeft.BackColor = Color.FromArgb(16, 18, 22);

            lblPlayerTag = new Label();
            lblPlayerTag.Text = "Müdür: -";
            lblPlayerTag.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblPlayerTag.ForeColor = Color.FromArgb(46, 204, 113);
            lblPlayerTag.Location = new Point(10, 6);
            lblPlayerTag.AutoSize = true;
            pnlHeaderLeft.Controls.Add(lblPlayerTag);

            Label lblTitleBadge = new Label();
            lblTitleBadge.Text = "CARBON ENERGY";
            lblTitleBadge.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblTitleBadge.ForeColor = Color.FromArgb(46, 204, 113);
            lblTitleBadge.Location = new Point(10, 26);
            lblTitleBadge.AutoSize = true;
            pnlHeaderLeft.Controls.Add(lblTitleBadge);

            Label lblSubTitle = new Label();
            lblSubTitle.Text = "Maden & İklim Parkı";
            lblSubTitle.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            lblSubTitle.ForeColor = Color.FromArgb(150, 155, 165);
            lblSubTitle.Location = new Point(12, 48);
            lblSubTitle.AutoSize = true;
            pnlHeaderLeft.Controls.Add(lblSubTitle);

            Panel pnlHeaderRight = new Panel();
            pnlHeaderRight.Dock = DockStyle.Right;
            pnlHeaderRight.Width = 224;
            pnlHeaderRight.BackColor = Color.FromArgb(16, 18, 22);

            btnNextMonth = new Button();
            btnNextMonth.Text = "⏭️ AY ATLA";
            btnNextMonth.Size = new Size(82, 54);
            btnNextMonth.Location = new Point(0, 8);
            btnNextMonth.FlatStyle = FlatStyle.Flat;
            btnNextMonth.FlatAppearance.BorderColor = Color.FromArgb(46, 204, 113);
            btnNextMonth.FlatAppearance.BorderSize = 2;
            btnNextMonth.BackColor = Color.FromArgb(32, 70, 48);
            btnNextMonth.ForeColor = Color.White;
            btnNextMonth.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            btnNextMonth.Cursor = Cursors.Hand;
            btnNextMonth.Click += new EventHandler(BtnNextMonth_Click);
            pnlHeaderRight.Controls.Add(btnNextMonth);

            btnSpeed = new Button();
            btnSpeed.Text = "⏩ 2x";
            btnSpeed.Size = new Size(48, 24);
            btnSpeed.Location = new Point(88, 8);
            btnSpeed.FlatStyle = FlatStyle.Flat;
            btnSpeed.BackColor = Color.FromArgb(35, 38, 45);
            btnSpeed.ForeColor = Color.White;
            btnSpeed.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            btnSpeed.Click += new EventHandler(BtnSpeed_Click);
            pnlHeaderRight.Controls.Add(btnSpeed);

            btnPause = new Button();
            btnPause.Text = "⏸ Dur";
            btnPause.Size = new Size(48, 24);
            btnPause.Location = new Point(88, 38);
            btnPause.FlatStyle = FlatStyle.Flat;
            btnPause.BackColor = Color.FromArgb(35, 38, 45);
            btnPause.ForeColor = Color.White;
            btnPause.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            btnPause.Click += new EventHandler(BtnPause_Click);
            pnlHeaderRight.Controls.Add(btnPause);

            btnSound = new Button();
            btnSound.Text = "🔊";
            btnSound.Size = new Size(34, 24);
            btnSound.Location = new Point(140, 8);
            btnSound.FlatStyle = FlatStyle.Flat;
            btnSound.BackColor = Color.FromArgb(35, 38, 45);
            btnSound.ForeColor = Color.White;
            btnSound.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            btnSound.Click += new EventHandler(BtnSound_Click);
            pnlHeaderRight.Controls.Add(btnSound);

            btnMainMenu = new Button();
            btnMainMenu.Text = "Menü";
            btnMainMenu.Size = new Size(34, 24);
            btnMainMenu.Location = new Point(140, 38);
            btnMainMenu.FlatStyle = FlatStyle.Flat;
            btnMainMenu.BackColor = Color.FromArgb(50, 35, 35);
            btnMainMenu.ForeColor = Color.FromArgb(231, 76, 60);
            btnMainMenu.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            btnMainMenu.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                if (model != null) model.IsPaused = true;
                monthTimer.Stop();
                SoundEngine.PlayClick();
                ShowScreen(GameScreen.MainMenu);
            });
            pnlHeaderRight.Controls.Add(btnMainMenu);

            btnSave = new Button();
            btnSave.Text = "💾";
            btnSave.Size = new Size(34, 24);
            btnSave.Location = new Point(178, 8);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.BackColor = Color.FromArgb(30, 48, 38);
            btnSave.ForeColor = Color.FromArgb(46, 204, 113);
            btnSave.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            btnSave.Cursor = Cursors.Hand;
            btnSave.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                if (model != null)
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "savegame.dat");
                    if (model.SaveToFile(path))
                    {
                        SoundEngine.PlayCash();
                        ShowNotification("💾 Oyun Başarıyla Kaydedildi! (" + model.GetCurrentDateString() + ")", Color.FromArgb(46, 204, 113));
                    }
                }
            });
            pnlHeaderRight.Controls.Add(btnSave);

            btnMusic = new Button();
            btnMusic.Text = "🎵";
            btnMusic.Size = new Size(34, 24);
            btnMusic.Location = new Point(178, 38);
            btnMusic.FlatStyle = FlatStyle.Flat;
            btnMusic.BackColor = SoundEngine.MusicEnabled ? Color.FromArgb(35, 38, 45) : Color.FromArgb(60, 40, 40);
            btnMusic.ForeColor = Color.White;
            btnMusic.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            btnMusic.Cursor = Cursors.Hand;
            btnMusic.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                SoundEngine.MusicEnabled = !SoundEngine.MusicEnabled;
                btnMusic.BackColor = SoundEngine.MusicEnabled ? Color.FromArgb(35, 38, 45) : Color.FromArgb(60, 40, 40);
                SoundEngine.PlayClick();
            });
            pnlHeaderRight.Controls.Add(btnMusic);

            TableLayoutPanel tblHud = new TableLayoutPanel();
            tblHud.Dock = DockStyle.Fill;
            tblHud.RowCount = 1;
            tblHud.ColumnCount = 6;
            for (int i = 0; i < 6; i++)
            {
                tblHud.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666f));
            }
            tblHud.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblHud.Padding = new Padding(2, 0, 2, 0);
            tblHud.Margin = new Padding(0);

            lblDate = CreateHudCard(0, "ZAMAN", "Ay 1 / 60", Color.FromArgb(200, 205, 215));
            lblBudget = CreateHudCard(1, "KASA (BÜTÇE)", "₺65,000", Color.FromArgb(46, 204, 113));
            lblEnergy = CreateHudCard(2, "ENERJİ DENGESİ", "750 / 800 MWh", Color.FromArgb(241, 196, 15));
            lblCarbon = CreateHudCard(3, "NET KARBON", "880 t CO₂/ay", Color.FromArgb(231, 76, 60));
            lblThreat = CreateHudCard(4, "İKLİM TEHDİDİ", "%22", Color.FromArgb(230, 126, 34));
            lblApproval = CreateHudCard(5, "HALK DESTEĞİ", "%68", Color.FromArgb(52, 152, 219));

            for (int i = 0; i < 6; i++)
            {
                tblHud.Controls.Add(hudCards[i], i, 0);
            }

            headerPanel.Controls.Add(tblHud);
            headerPanel.Controls.Add(pnlHeaderRight);
            headerPanel.Controls.Add(pnlHeaderLeft);

            ToolTip tip = new ToolTip();
            tip.AutoPopDelay = 5000;
            tip.InitialDelay = 300;
            tip.SetToolTip(btnNextMonth, "⏭️ 1 Ay İleri Sar: Sözleşmeler teslim edilir, vergiler tahsil edilir.");
            tip.SetToolTip(btnSpeed, "⏩ Zaman Akış Hızını Değiştir (1x / 2x)");
            tip.SetToolTip(btnPause, "⏸️ Zamanı Duraklat / Devam Ettir");
            tip.SetToolTip(btnSound, "🔊 Ses Efektlerini Aç / Kapat");
            tip.SetToolTip(btnMainMenu, "🚪 Ana Menüye Dön");
            tip.SetToolTip(btnSave, "💾 Oyunu Kaydet (Daha sonra devam edebilirsin)");
            tip.SetToolTip(btnMusic, "🎵 Dinlendirici Ambiyans Müziğini Aç / Kapat");
        }

        private Label CreateHudCard(int index, string title, string initialValue, Color accentColor)
        {
            Panel card = new Panel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(2, 4, 2, 4);
            card.BackColor = Color.FromArgb(26, 29, 36);

            Label lblT = new Label();
            lblT.Text = title;
            lblT.Font = new Font("Segoe UI", 6.8f, FontStyle.Bold);
            lblT.ForeColor = Color.FromArgb(130, 135, 145);
            lblT.Dock = DockStyle.Top;
            lblT.Height = 16;
            lblT.Padding = new Padding(4, 2, 0, 0);
            card.Controls.Add(lblT);

            Label lblV = new Label();
            lblV.Text = initialValue;
            lblV.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            lblV.ForeColor = accentColor;
            lblV.Dock = DockStyle.Fill;
            lblV.Padding = new Padding(4, 0, 0, 0);
            lblV.TextAlign = ContentAlignment.TopLeft;
            card.Controls.Add(lblV);

            lblV.BringToFront();

            if (index >= 0 && index < hudCards.Length) hudCards[index] = card;
            return lblV;
        }

        private void BuildTabBarControls()
        {
            tabNavPanel.Padding = new Padding(4, 2, 4, 2);

            btnInGameGuide = new Button();
            btnInGameGuide.Text = "📖 Nasıl Oynanır?";
            btnInGameGuide.UseMnemonic = false;
            btnInGameGuide.Dock = DockStyle.Right;
            btnInGameGuide.Width = 140;
            btnInGameGuide.FlatStyle = FlatStyle.Flat;
            btnInGameGuide.FlatAppearance.BorderColor = Color.FromArgb(241, 196, 15);
            btnInGameGuide.FlatAppearance.BorderSize = 1;
            btnInGameGuide.BackColor = Color.FromArgb(35, 40, 50);
            btnInGameGuide.ForeColor = Color.FromArgb(241, 196, 15);
            btnInGameGuide.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            btnInGameGuide.Cursor = Cursors.Hand;
            btnInGameGuide.Margin = new Padding(2, 2, 2, 2);
            btnInGameGuide.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                if (model != null) model.IsPaused = true;
                SoundEngine.PlayClick();
                ShowScreen(GameScreen.HowToPlay);
            });

            TableLayoutPanel tblTabs = new TableLayoutPanel();
            tblTabs.Dock = DockStyle.Fill;
            tblTabs.RowCount = 1;
            tblTabs.ColumnCount = 5;
            for (int i = 0; i < 5; i++)
            {
                tblTabs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            }
            tblTabs.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblTabs.Padding = new Padding(0);
            tblTabs.Margin = new Padding(0);

            btnTabOps = CreateTabButton("🎮 Üretim & Sözleşme", "ops");
            btnTabUpgrades = CreateTabButton("🔬 Teknoloji", "upgrades");
            btnTabDilemmas = CreateTabButton("⚖️ Kriz Masası", "dilemmas");
            btnTabClimate = CreateTabButton("🌍 İklim Bilimi", "climate");
            btnTabAchievements = CreateTabButton("🏆 Kupa Odası", "achievements");

            tblTabs.Controls.Add(btnTabOps, 0, 0);
            tblTabs.Controls.Add(btnTabUpgrades, 1, 0);
            tblTabs.Controls.Add(btnTabDilemmas, 2, 0);
            tblTabs.Controls.Add(btnTabClimate, 3, 0);
            tblTabs.Controls.Add(btnTabAchievements, 4, 0);

            tabNavPanel.Controls.Add(tblTabs);
            tabNavPanel.Controls.Add(btnInGameGuide);
        }

        private Button CreateTabButton(string text, string tabKey)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.UseMnemonic = false;
            btn.Dock = DockStyle.Fill;
            btn.Margin = new Padding(2, 2, 2, 2);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 8.6f, FontStyle.Bold);
            btn.BackColor = Color.FromArgb(35, 38, 45);
            btn.ForeColor = Color.FromArgb(180, 185, 195);
            btn.Tag = tabKey;
            btn.Cursor = Cursors.Hand;
            btn.Click += new EventHandler(TabButton_Click);
            return btn;
        }

        private void TabButton_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                SwitchTab(btn.Tag.ToString());
                SoundEngine.PlayClick();
            }
        }

        private void SwitchTab(string tabKey)
        {
            activeTab = tabKey;

            panelOperations.Visible = false;
            panelUpgrades.Visible = false;
            panelDilemmas.Visible = false;
            panelClimateHub.Visible = false;
            panelAchievements.Visible = false;

            if (tabKey == "ops")
            {
                panelOperations.Visible = true;
                panelOperations.BringToFront();
            }
            else if (tabKey == "upgrades")
            {
                panelUpgrades.Visible = true;
                panelUpgrades.BringToFront();
                RefreshUpgradesPanel();
            }
            else if (tabKey == "dilemmas")
            {
                panelDilemmas.Visible = true;
                panelDilemmas.BringToFront();
                RefreshDilemmasPanel();
            }
            else if (tabKey == "climate")
            {
                panelClimateHub.Visible = true;
                panelClimateHub.BringToFront();
                RefreshClimateHub();
                if (chartPanel != null) chartPanel.Invalidate();
            }
            else if (tabKey == "achievements")
            {
                panelAchievements.Visible = true;
                panelAchievements.BringToFront();
                RefreshAchievementsPanel();
            }

            StyleTabButton(btnTabOps, tabKey == "ops");
            StyleTabButton(btnTabUpgrades, tabKey == "upgrades");
            StyleTabButton(btnTabDilemmas, tabKey == "dilemmas");
            StyleTabButton(btnTabClimate, tabKey == "climate");
            StyleTabButton(btnTabAchievements, tabKey == "achievements");
        }

        private void StyleTabButton(Button btn, bool active)
        {
            if (active)
            {
                btn.BackColor = Color.FromArgb(46, 204, 113);
                btn.ForeColor = Color.Black;
            }
            else
            {
                btn.BackColor = Color.FromArgb(35, 38, 45);
                btn.ForeColor = Color.FromArgb(180, 185, 195);
            }
        }

        // ==========================================
        // TAB 1: OPERATIONS & CONTRACTS & MANUAL WORK
        // ==========================================
        private void BuildOperationsTab()
        {
            panelOperations = new Panel();
            panelOperations.Dock = DockStyle.Fill;
            panelOperations.BackColor = Color.FromArgb(24, 26, 30);
            panelOperations.AutoScroll = false;

            TableLayoutPanel tblOps = new TableLayoutPanel();
            tblOps.Dock = DockStyle.Fill;
            tblOps.RowCount = 1;
            tblOps.ColumnCount = 3;
            tblOps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblOps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            tblOps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            tblOps.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblOps.Padding = new Padding(2);
            tblOps.Margin = new Padding(0);

            // Left Box: Coal Boiler & Resource Stock
            boxSlider = new Panel();
            boxSlider.Dock = DockStyle.Fill;
            boxSlider.Margin = new Padding(4);
            boxSlider.BackColor = Color.FromArgb(30, 33, 40);
            boxSlider.AutoScroll = false;

            Label lblSliderHeader = new Label();
            lblSliderHeader.Text = "KÖMÜR KAZANI & MADEN REZERVİ";
            lblSliderHeader.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblSliderHeader.ForeColor = Color.FromArgb(241, 196, 15);
            lblSliderHeader.Location = new Point(10, 6);
            lblSliderHeader.AutoSize = true;
            boxSlider.Controls.Add(lblSliderHeader);

            tbCoalSlider = new TrackBar();
            tbCoalSlider.Minimum = 40;
            tbCoalSlider.Maximum = 150;
            tbCoalSlider.Value = 100;
            tbCoalSlider.TickFrequency = 10;
            tbCoalSlider.Location = new Point(10, 28);
            tbCoalSlider.Size = new Size(280, 28);
            tbCoalSlider.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            tbCoalSlider.ValueChanged += new EventHandler(TbCoalSlider_ValueChanged);
            boxSlider.Controls.Add(tbCoalSlider);

            lblSliderVal = new Label();
            lblSliderVal.Text = "%100 - Standart Üretim";
            lblSliderVal.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblSliderVal.ForeColor = Color.FromArgb(46, 204, 113);
            lblSliderVal.Location = new Point(10, 58);
            lblSliderVal.AutoSize = true;
            boxSlider.Controls.Add(lblSliderVal);

            lblCoalStockBar = new Label();
            lblCoalStockBar.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            lblCoalStockBar.ForeColor = Color.FromArgb(243, 156, 18);
            lblCoalStockBar.Location = new Point(10, 78);
            lblCoalStockBar.Size = new Size(280, 18);
            lblCoalStockBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            boxSlider.Controls.Add(lblCoalStockBar);

            lblStaminaBar = new Label();
            lblStaminaBar.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            lblStaminaBar.ForeColor = Color.FromArgb(52, 152, 219);
            lblStaminaBar.Location = new Point(10, 98);
            lblStaminaBar.Size = new Size(280, 18);
            lblStaminaBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            boxSlider.Controls.Add(lblStaminaBar);

            lblStatsDetails = new Label();
            lblStatsDetails.Font = new Font("Segoe UI", 8.2f, FontStyle.Regular);
            lblStatsDetails.ForeColor = Color.FromArgb(200, 205, 215);
            lblStatsDetails.Location = new Point(10, 118);
            lblStatsDetails.Size = new Size(280, 56);
            lblStatsDetails.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            boxSlider.Controls.Add(lblStatsDetails);

            lblTelemetryInfo = new Label();
            lblTelemetryInfo.Font = new Font("Segoe UI", 8.2f, FontStyle.Regular);
            lblTelemetryInfo.ForeColor = Color.FromArgb(170, 215, 240);
            lblTelemetryInfo.Location = new Point(10, 176);
            lblTelemetryInfo.Size = new Size(280, 80);
            lblTelemetryInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblTelemetryInfo.Text = "Telemetri yükleniyor...";
            boxSlider.Controls.Add(lblTelemetryInfo);

            tblOps.Controls.Add(boxSlider, 0, 0);

            // Center Box: DIRECTOR'S MANUAL ACTIONS
            boxActions = new Panel();
            boxActions.Dock = DockStyle.Fill;
            boxActions.Margin = new Padding(4);
            boxActions.BackColor = Color.FromArgb(30, 33, 40);
            boxActions.AutoScroll = false;

            Label lblActionHeader = new Label();
            lblActionHeader.Text = "⚡ MÜDÜRÜN MANUEL MÜDAHALE MASASI";
            lblActionHeader.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblActionHeader.ForeColor = Color.FromArgb(46, 204, 113);
            lblActionHeader.Location = new Point(10, 6);
            lblActionHeader.AutoSize = true;
            boxActions.Controls.Add(lblActionHeader);

            actionButtonsList.Clear();

            CreateActionButton(boxActions, "⛏️ Vardiya Başlat: +45 Ton Kömür Çıkar (-₺1,200)", 10, 26, 280, 28,
                Color.FromArgb(241, 196, 15), delegate
                {
                    string msg = model.ActionMineCoalShift();
                    renderer.AddFloatingText(msg, 180, 160, Color.FromArgb(241, 196, 15));
                    ShowNotification(msg, Color.FromArgb(241, 196, 15));
                    AddActionLog(msg);
                    UpdateHudLabels();
                });

            CreateActionButton(boxActions, "⚡ Borsaya Anlık Enerji Sat (Spot Satış)", 10, 56, 280, 28,
                Color.FromArgb(243, 156, 18), delegate
                {
                    string msg = model.ActionSellSpotEnergy();
                    renderer.AddFloatingText(msg, 600, 140, Color.FromArgb(243, 156, 18));
                    ShowNotification(msg, Color.FromArgb(46, 204, 113));
                    AddActionLog(msg);
                    UpdateHudLabels();
                });

            CreateActionButton(boxActions, "🌲 Toplu Fidan Dikimi (-₺4,000, +20 t Yutak)", 10, 86, 280, 28,
                Color.FromArgb(46, 204, 113), delegate
                {
                    string msg = model.ActionPlantTrees();
                    renderer.AddFloatingText(msg, 60, 210, Color.FromArgb(100, 240, 140));
                    ShowNotification(msg, Color.FromArgb(46, 204, 113));
                    AddActionLog(msg);
                    UpdateHudLabels();
                });

            CreateActionButton(boxActions, "💨 Kimyasal Baca Yıkaması (-₺2,800, -40 t CO₂)", 10, 116, 280, 28,
                Color.FromArgb(52, 152, 219), delegate
                {
                    string msg = model.ActionChemicalScrub();
                    renderer.AddFloatingText(msg, 440, 130, Color.FromArgb(90, 200, 255));
                    ShowNotification(msg, Color.FromArgb(52, 152, 219));
                    AddActionLog(msg);
                    UpdateHudLabels();
                });

            CreateActionButton(boxActions, "🩺 İşçi Primi & Dinlenme (-₺3,500, +%35 Kondisyon)", 10, 146, 280, 28,
                Color.FromArgb(155, 89, 182), delegate
                {
                    string msg = model.ActionWorkerBonus();
                    renderer.AddFloatingText(msg, 380, 230, Color.FromArgb(210, 140, 255));
                    ShowNotification(msg, Color.FromArgb(155, 89, 182));
                    AddActionLog(msg);
                    UpdateHudLabels();
                });

            CreateActionButton(boxActions, "📢 Basın Toplantısı & Rapor (-₺1,800, +%8 Halk)", 10, 176, 280, 28,
                Color.FromArgb(230, 126, 34), delegate
                {
                    string msg = model.ActionPressConference();
                    renderer.AddFloatingText(msg, 500, 180, Color.FromArgb(240, 160, 70));
                    ShowNotification(msg, Color.FromArgb(230, 126, 34));
                    AddActionLog(msg);
                    UpdateHudLabels();
                });

            lblActionLogHistory = new Label();
            lblActionLogHistory.Font = new Font("Segoe UI", 8.2f, FontStyle.Regular);
            lblActionLogHistory.ForeColor = Color.FromArgb(190, 210, 200);
            lblActionLogHistory.Location = new Point(10, 208);
            lblActionLogHistory.Size = new Size(280, 75);
            lblActionLogHistory.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblActionLogHistory.Text = "📋 SON SAHA OPERASYONLARI GÜNLÜĞÜ:\n• Henüz manuel operasyon yürütülmedi.";
            boxActions.Controls.Add(lblActionLogHistory);

            tblOps.Controls.Add(boxActions, 1, 0);

            // Right Box: CONTRACTS & SPOT MARKET
            boxContracts = new Panel();
            boxContracts.Dock = DockStyle.Fill;
            boxContracts.Margin = new Padding(4);
            boxContracts.BackColor = Color.FromArgb(30, 33, 40);
            boxContracts.AutoScroll = false;

            Label lblContractHeader = new Label();
            lblContractHeader.Text = "📋 ENERJİ SÖZLEŞMELERİ & PİYASA VERİLERİ";
            lblContractHeader.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblContractHeader.ForeColor = Color.FromArgb(52, 152, 219);
            lblContractHeader.Location = new Point(10, 6);
            lblContractHeader.AutoSize = true;
            boxContracts.Controls.Add(lblContractHeader);

            lblContractsInfo = new Label();
            lblContractsInfo.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            lblContractsInfo.ForeColor = Color.FromArgb(220, 225, 235);
            lblContractsInfo.Location = new Point(10, 26);
            lblContractsInfo.Size = new Size(280, 60);
            lblContractsInfo.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            boxContracts.Controls.Add(lblContractsInfo);

            lblContractCommitmentDetails = new Label();
            lblContractCommitmentDetails.Font = new Font("Segoe UI", 8.2f, FontStyle.Regular);
            lblContractCommitmentDetails.ForeColor = Color.FromArgb(241, 196, 15);
            lblContractCommitmentDetails.Location = new Point(10, 88);
            lblContractCommitmentDetails.Size = new Size(280, 36);
            lblContractCommitmentDetails.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblContractCommitmentDetails.Text = "Sözleşme oranları hesaplanıyor...";
            boxContracts.Controls.Add(lblContractCommitmentDetails);

            lblMarketSpotBourse = new Label();
            lblMarketSpotBourse.Font = new Font("Segoe UI", 8.2f, FontStyle.Regular);
            lblMarketSpotBourse.ForeColor = Color.FromArgb(130, 220, 180);
            lblMarketSpotBourse.Location = new Point(10, 126);
            lblMarketSpotBourse.Size = new Size(280, 36);
            lblMarketSpotBourse.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblMarketSpotBourse.Text = "Borsa verisi alınıyor...";
            boxContracts.Controls.Add(lblMarketSpotBourse);

            lineDivContracts = new Panel();
            lineDivContracts.Location = new Point(10, 164);
            lineDivContracts.Size = new Size(280, 1);
            lineDivContracts.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lineDivContracts.BackColor = Color.FromArgb(50, 55, 65);
            boxContracts.Controls.Add(lineDivContracts);

            lblNewsTicker = new Label();
            lblNewsTicker.Font = new Font("Segoe UI", 8.2f, FontStyle.Italic);
            lblNewsTicker.ForeColor = Color.FromArgb(241, 196, 15);
            lblNewsTicker.Location = new Point(10, 168);
            lblNewsTicker.Size = new Size(280, 65);
            lblNewsTicker.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblNewsTicker.Text = "Haber akışı bekleniyor...";
            boxContracts.Controls.Add(lblNewsTicker);

            tblOps.Controls.Add(boxContracts, 2, 0);

            panelOperations.Controls.Add(tblOps);
            contentPanel.Controls.Add(panelOperations);
        }

        private void AddActionLog(string msg)
        {
            if (model == null) return;
            string entry = string.Format("[Ay {0}] {1}", model.CurrentMonth, msg);
            recentActionLogs.Insert(0, entry);
            if (recentActionLogs.Count > 5) recentActionLogs.RemoveAt(recentActionLogs.Count - 1);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("📋 SON SAHA OPERASYONLARI GÜNLÜĞÜ:");
            for (int i = 0; i < recentActionLogs.Count; i++)
            {
                sb.AppendLine("• " + recentActionLogs[i]);
            }
            if (lblActionLogHistory != null)
            {
                lblActionLogHistory.Text = sb.ToString();
            }
        }

        private void CreateActionButton(Panel parent, string text, int x, int y, int w, int h, Color borderCol, Action onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.UseMnemonic = false;
            btn.Location = new Point(x, y);
            btn.Size = new Size(w, h);
            btn.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = borderCol;
            btn.FlatAppearance.BorderSize = 1;
            btn.BackColor = Color.FromArgb(36, 40, 50);
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(6, 0, 0, 0);

            btn.MouseEnter += new EventHandler(delegate(object s, EventArgs ev)
            {
                btn.BackColor = borderCol;
                btn.ForeColor = Color.Black;
            });
            btn.MouseLeave += new EventHandler(delegate(object s, EventArgs ev)
            {
                btn.BackColor = Color.FromArgb(36, 40, 50);
                btn.ForeColor = Color.White;
            });
            btn.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                onClick();
            });

            parent.Controls.Add(btn);
            actionButtonsList.Add(btn);
        }

        private void TbCoalSlider_ValueChanged(object sender, EventArgs e)
        {
            if (model == null) return;
            model.CoalProductionSlider = tbCoalSlider.Value / 100.0;
            model.RecalculateStats();

            string status;
            if (tbCoalSlider.Value < 75)
            {
                status = "Eko-Tasarruf Modu (Düşük Kömür Tüketimi)";
                lblSliderVal.ForeColor = Color.FromArgb(46, 204, 113);
            }
            else if (tbCoalSlider.Value <= 110)
            {
                status = "Dengeli Üretim";
                lblSliderVal.ForeColor = Color.FromArgb(241, 196, 15);
            }
            else
            {
                status = "AŞIRI KÖMÜR YÜKLEMESİ! (Yüksek Karbon Vergisi!)";
                lblSliderVal.ForeColor = Color.FromArgb(231, 76, 60);
            }

            lblSliderVal.Text = string.Format("%{0} - {1}", tbCoalSlider.Value, status);
            UpdateHudLabels();
        }

        // ==========================================
        // TAB 2: UPGRADES
        // ==========================================
        private void BuildUpgradesTab()
        {
            panelUpgrades = new Panel();
            panelUpgrades.Dock = DockStyle.Fill;
            panelUpgrades.BackColor = Color.FromArgb(24, 26, 30);

            flowUpgrades = new FlowLayoutPanel();
            flowUpgrades.Dock = DockStyle.Fill;
            flowUpgrades.AutoScroll = true;
            flowUpgrades.Padding = new Padding(6);
            flowUpgrades.BackColor = Color.FromArgb(24, 26, 30);

            panelUpgrades.Controls.Add(flowUpgrades);
            contentPanel.Controls.Add(panelUpgrades);
        }

        private void RefreshUpgradesPanel()
        {
            if (model == null) return;

            flowUpgrades.SuspendLayout();
            flowUpgrades.Controls.Clear();

            for (int i = 0; i < model.Upgrades.Count; i++)
            {
                UpgradeItem up = model.Upgrades[i];
                Panel card = new Panel();
                card.Size = new Size(365, 142);
                card.BackColor = Color.FromArgb(30, 33, 40);
                card.Margin = new Padding(6);

                Label lblName = new Label();
                lblName.Text = string.Format("{0} {1}", up.IconSymbol, up.Name);
                lblName.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                lblName.ForeColor = Color.White;
                lblName.Location = new Point(10, 8);
                lblName.Size = new Size(260, 22);
                card.Controls.Add(lblName);

                Label lblLevel = new Label();
                lblLevel.Text = up.IsMaxed() ? "MAX SEVİYE" : string.Format("Svye {0}/{1}", up.Level, up.MaxLevel);
                lblLevel.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
                lblLevel.ForeColor = up.IsMaxed() ? Color.FromArgb(241, 196, 15) : Color.FromArgb(46, 204, 113);
                lblLevel.Location = new Point(275, 8);
                lblLevel.AutoSize = true;
                card.Controls.Add(lblLevel);

                Label lblCat = new Label();
                lblCat.Text = up.Category;
                lblCat.Font = new Font("Segoe UI", 7.5f, FontStyle.Italic);
                lblCat.ForeColor = Color.FromArgb(120, 190, 255);
                lblCat.Location = new Point(10, 30);
                lblCat.AutoSize = true;
                card.Controls.Add(lblCat);

                Label lblDesc = new Label();
                lblDesc.Text = up.Description;
                lblDesc.Font = new Font("Segoe UI", 7.8f, FontStyle.Regular);
                lblDesc.ForeColor = Color.FromArgb(170, 175, 185);
                lblDesc.Location = new Point(10, 48);
                lblDesc.Size = new Size(345, 30);
                card.Controls.Add(lblDesc);

                string impact = "";
                if (up.EmissionReductionPercent > 0) impact += string.Format("Salım: -%{0:0} | ", up.EmissionReductionPercent * 100);
                if (up.CleanEnergyOutputMWh > 0) impact += string.Format("Temiz: +{0:0} MWh | ", up.CleanEnergyOutputMWh);
                if (up.ForestAbsorptionTons > 0) impact += string.Format("Yutak: +{0:0} t | ", up.ForestAbsorptionTons);
                impact += string.Format("Destek: +%{0:0}", up.PublicApprovalBonus);

                Label lblImpact = new Label();
                lblImpact.Text = impact;
                lblImpact.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
                lblImpact.ForeColor = Color.FromArgb(100, 210, 255);
                lblImpact.Location = new Point(10, 80);
                lblImpact.Size = new Size(345, 18);
                card.Controls.Add(lblImpact);

                Label lblMaint = new Label();
                lblMaint.Text = string.Format("Bakım: ₺{0:N0}/ay", up.MonthlyMaintenance);
                lblMaint.Font = new Font("Segoe UI", 7.5f, FontStyle.Regular);
                lblMaint.ForeColor = Color.FromArgb(140, 145, 155);
                lblMaint.Location = new Point(10, 108);
                lblMaint.AutoSize = true;
                card.Controls.Add(lblMaint);

                double cost = up.GetCurrentCost();
                Button btnBuy = new Button();
                btnBuy.Size = new Size(180, 32);
                btnBuy.Location = new Point(175, 102);
                btnBuy.FlatStyle = FlatStyle.Flat;
                btnBuy.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                btnBuy.Cursor = Cursors.Hand;

                if (up.IsMaxed())
                {
                    btnBuy.Text = "TAMAMLANDI";
                    btnBuy.BackColor = Color.FromArgb(40, 43, 50);
                    btnBuy.ForeColor = Color.FromArgb(100, 105, 115);
                    btnBuy.Enabled = false;
                }
                else
                {
                    btnBuy.Text = string.Format("Yatırım: ₺{0:N0}", cost);
                    btnBuy.Tag = up.Id;
                    btnBuy.Click += new EventHandler(BtnBuyUpgrade_Click);

                    if (model.Budget >= cost)
                    {
                        btnBuy.BackColor = Color.FromArgb(46, 204, 113);
                        btnBuy.ForeColor = Color.Black;
                        btnBuy.Enabled = true;
                    }
                    else
                    {
                        btnBuy.BackColor = Color.FromArgb(55, 40, 40);
                        btnBuy.ForeColor = Color.FromArgb(180, 80, 70);
                        btnBuy.Enabled = false;
                    }
                }
                card.Controls.Add(btnBuy);

                flowUpgrades.Controls.Add(card);
            }

            flowUpgrades.ResumeLayout(true);
        }

        private void BtnBuyUpgrade_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag != null && model != null)
            {
                string upId = btn.Tag.ToString();
                if (model.BuyUpgrade(upId))
                {
                    string upName = upId;
                    for (int i = 0; i < model.Upgrades.Count; i++)
                    {
                        if (model.Upgrades[i].Id == upId)
                        {
                            upName = model.Upgrades[i].Name;
                            break;
                        }
                    }

                    renderer.AddFloatingText(upName + " Kuruldu! ⭐", 500, 130, Color.FromArgb(241, 196, 15));
                    model.AddEvent(upName + " yatırımı yapıldı!");
                    ShowNotification(upName + " kuruldu! Temiz enerji kapasitesi arttı.", Color.FromArgb(46, 204, 113));
                    RefreshUpgradesPanel();
                    UpdateHudLabels();
                }
            }
        }

        // ==========================================
        // TAB 3: CRISIS DESK & HISTORICAL ARCHIVE
        // ==========================================
        private void BuildDilemmasTab()
        {
            panelDilemmas = new Panel();
            panelDilemmas.Dock = DockStyle.Fill;
            panelDilemmas.BackColor = Color.FromArgb(24, 26, 30);
            panelDilemmas.AutoScroll = false;

            TableLayoutPanel tblDilemmas = new TableLayoutPanel();
            tblDilemmas.Dock = DockStyle.Fill;
            tblDilemmas.RowCount = 1;
            tblDilemmas.ColumnCount = 2;
            tblDilemmas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblDilemmas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tblDilemmas.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblDilemmas.Padding = new Padding(2);
            tblDilemmas.Margin = new Padding(0);

            // Left Side: Active Crisis or Routine Inspection
            panelActiveDilemmaBox = new Panel();
            panelActiveDilemmaBox.Dock = DockStyle.Fill;
            panelActiveDilemmaBox.Margin = new Padding(4);
            panelActiveDilemmaBox.BackColor = Color.FromArgb(30, 33, 40);
            panelActiveDilemmaBox.Padding = new Padding(12);

            lblDilemmaSender = new Label();
            lblDilemmaSender.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            lblDilemmaSender.ForeColor = Color.FromArgb(52, 152, 219);
            lblDilemmaSender.Location = new Point(12, 10);
            lblDilemmaSender.Size = new Size(575, 20);
            lblDilemmaSender.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelActiveDilemmaBox.Controls.Add(lblDilemmaSender);

            lblDilemmaTitle = new Label();
            lblDilemmaTitle.Font = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            lblDilemmaTitle.ForeColor = Color.FromArgb(243, 156, 18);
            lblDilemmaTitle.Location = new Point(12, 30);
            lblDilemmaTitle.Size = new Size(575, 26);
            lblDilemmaTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelActiveDilemmaBox.Controls.Add(lblDilemmaTitle);

            lblDilemmaDesc = new Label();
            lblDilemmaDesc.Font = new Font("Segoe UI", 9.2f, FontStyle.Regular);
            lblDilemmaDesc.ForeColor = Color.FromArgb(215, 220, 230);
            lblDilemmaDesc.Location = new Point(12, 58);
            lblDilemmaDesc.Size = new Size(575, 48);
            lblDilemmaDesc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            panelActiveDilemmaBox.Controls.Add(lblDilemmaDesc);

            // Flow layout for choices (A, B, C, D) or Risk Radar
            flowDilemmaChoices = new FlowLayoutPanel();
            flowDilemmaChoices.Location = new Point(12, 110);
            flowDilemmaChoices.Size = new Size(575, 140);
            flowDilemmaChoices.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            flowDilemmaChoices.AutoScroll = true;
            panelActiveDilemmaBox.Controls.Add(flowDilemmaChoices);

            lblRiskAnalysis = new Label();
            lblRiskAnalysis.Font = new Font("Segoe UI", 8.2f, FontStyle.Italic);
            lblRiskAnalysis.ForeColor = Color.FromArgb(160, 170, 185);
            lblRiskAnalysis.Location = new Point(12, 255);
            lblRiskAnalysis.Size = new Size(575, 34);
            lblRiskAnalysis.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelActiveDilemmaBox.Controls.Add(lblRiskAnalysis);

            tblDilemmas.Controls.Add(panelActiveDilemmaBox, 0, 0);

            // Right Side: Historical Decisions Archive
            boxArchive = new Panel();
            boxArchive.Dock = DockStyle.Fill;
            boxArchive.Margin = new Padding(4);
            boxArchive.BackColor = Color.FromArgb(30, 33, 40);
            boxArchive.Padding = new Padding(12);

            Label lblArchiveTitle = new Label();
            lblArchiveTitle.Text = "📜 ALINAN KRİZ KARARLARI & ŞİRKET ARŞİVİ";
            lblArchiveTitle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblArchiveTitle.ForeColor = Color.FromArgb(46, 204, 113);
            lblArchiveTitle.Location = new Point(12, 10);
            lblArchiveTitle.AutoSize = true;
            boxArchive.Controls.Add(lblArchiveTitle);

            listPastDilemmas = new ListBox();
            listPastDilemmas.Location = new Point(12, 34);
            listPastDilemmas.Size = new Size(530, 240);
            listPastDilemmas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listPastDilemmas.BackColor = Color.FromArgb(22, 25, 32);
            listPastDilemmas.ForeColor = Color.FromArgb(200, 210, 220);
            listPastDilemmas.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            listPastDilemmas.BorderStyle = BorderStyle.FixedSingle;
            boxArchive.Controls.Add(listPastDilemmas);

            tblDilemmas.Controls.Add(boxArchive, 1, 0);

            panelDilemmas.Controls.Add(tblDilemmas);
            contentPanel.Controls.Add(panelDilemmas);
        }

        private Panel CreateRiskCard(string title, double percent, Color barColor, int width)
        {
            Panel p = new Panel();
            p.Size = new Size(width, 26);
            p.Margin = new Padding(2, 2, 2, 2);
            p.BackColor = Color.FromArgb(24, 28, 36);

            Label lblT = new Label();
            lblT.Text = string.Format("{0}: %{1:0}", title, percent);
            lblT.Font = new Font("Segoe UI", 8.2f, FontStyle.Regular);
            lblT.ForeColor = percent > 50 ? Color.FromArgb(241, 196, 15) : Color.FromArgb(200, 205, 215);
            lblT.Location = new Point(6, 4);
            lblT.Size = new Size(Math.Max(180, width - 160), 18);
            p.Controls.Add(lblT);

            Panel bgBar = new Panel();
            bgBar.Location = new Point(Math.Max(185, width - 150), 6);
            bgBar.Size = new Size(140, 12);
            bgBar.BackColor = Color.FromArgb(40, 45, 55);

            Panel fillBar = new Panel();
            fillBar.Location = new Point(0, 0);
            int fillW = (int)(140.0 * (Math.Min(100.0, Math.Max(0.0, percent)) / 100.0));
            fillBar.Size = new Size(fillW, 12);
            fillBar.BackColor = percent > 60 ? Color.FromArgb(231, 76, 60) : barColor;
            bgBar.Controls.Add(fillBar);

            p.Controls.Add(bgBar);
            return p;
        }

        private Button CreateProactiveButton(string text, int width, Action onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.UseMnemonic = false;
            btn.Size = new Size(width, 34);
            btn.Margin = new Padding(2, 3, 2, 3);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Color.FromArgb(50, 58, 72);
            btn.BackColor = Color.FromArgb(32, 38, 50);
            btn.ForeColor = Color.White;
            btn.Font = new Font("Segoe UI", 8.2f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(8, 0, 0, 0);

            btn.MouseEnter += new EventHandler(delegate(object s, EventArgs ev)
            {
                btn.BackColor = Color.FromArgb(46, 204, 113);
                btn.ForeColor = Color.Black;
            });
            btn.MouseLeave += new EventHandler(delegate(object s, EventArgs ev)
            {
                btn.BackColor = Color.FromArgb(32, 38, 50);
                btn.ForeColor = Color.White;
            });
            btn.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                onClick();
            });
            return btn;
        }

        private void RefreshDilemmasPanel()
        {
            if (model == null) return;

            flowDilemmaChoices.SuspendLayout();
            flowDilemmaChoices.Controls.Clear();

            int itemW = Math.Max(480, flowDilemmaChoices.ClientSize.Width - 28);

            if (model.CurrentActiveDilemma != null)
            {
                DilemmaEvent d = model.CurrentActiveDilemma;
                lblDilemmaSender.Text = string.Format("🚨 BEKLEYEN ACİL DOSYA: {0} ({1})", d.CharacterName, d.CharacterRole);
                lblDilemmaTitle.Text = d.Title;
                lblDilemmaDesc.Text = d.Description;
                lblRiskAnalysis.Text = "UYARI: Verilen karar geri alınamaz ve hem bütçeyi hem iklim dengesini doğrudan etkiler!";

                for (int i = 0; i < d.Choices.Count; i++)
                {
                    DilemmaChoice ch = d.Choices[i];
                    Button btn = new Button();
                    btn.Text = string.Format("{0}) {1} [{2}]", ch.Key, ch.Title, ch.ConsequenceSummary);
                    btn.Size = new Size(itemW, 36);
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.BackColor = Color.FromArgb(36, 42, 54);
                    btn.ForeColor = Color.White;
                    btn.Font = new Font("Segoe UI", 8.2f, FontStyle.Bold);
                    btn.Tag = ch.Key;
                    btn.Cursor = Cursors.Hand;
                    btn.Click += new EventHandler(delegate(object s, EventArgs ev)
                    {
                        Button b = s as Button;
                        model.ApplyDilemmaChoiceByKey(b.Tag.ToString());
                        ShowNotification("Karar uygulandı!", Color.FromArgb(46, 204, 113));
                        RefreshDilemmasPanel();
                        UpdateHudLabels();
                    });
                    flowDilemmaChoices.Controls.Add(btn);
                }
            }
            else
            {
                lblDilemmaSender.Text = "🛡️ KURUMSAL RİSK RADARI & ÖNLEYİCİ MÜDAHALE MASASI";
                lblDilemmaTitle.Text = "Rutin İzleme ve Önleyici Kriz Masası";
                lblDilemmaDesc.Text = "Krizlerin kendiliğinden patlamasını beklemeden önleyici tedbir bütçesi kullanarak riskleri düşürün.";

                // Subtitle 1: Risk Indicators
                Label lblRiskSub1 = new Label();
                lblRiskSub1.Text = "📊 ANLIK ŞİRKET TEHDİT & ALARM GÖSTERGELERİ:";
                lblRiskSub1.Font = new Font("Segoe UI", 9.2f, FontStyle.Bold);
                lblRiskSub1.ForeColor = Color.FromArgb(241, 196, 15);
                lblRiskSub1.Size = new Size(itemW, 22);
                flowDilemmaChoices.Controls.Add(lblRiskSub1);

                double riskGov = model.GetMinistryInspectionRisk();
                double riskStrike = model.GetWorkerStrikeRisk();
                double riskProtest = model.GetPublicProtestRisk();
                double riskBankrupt = model.GetBankruptcyRisk();
                double riskDisaster = model.GetMinedustDisasterRisk();

                flowDilemmaChoices.Controls.Add(CreateRiskCard("🏛️ Bakanlık Çevre Denetimi & Ceza Riski", riskGov, Color.FromArgb(231, 76, 60), itemW));
                flowDilemmaChoices.Controls.Add(CreateRiskCard("👷 İşçi & Sendika Grev Tehdidi", riskStrike, Color.FromArgb(243, 156, 18), itemW));
                flowDilemmaChoices.Controls.Add(CreateRiskCard("📢 Bölge Halkı Protesto & Boykot Riski", riskProtest, Color.FromArgb(230, 126, 34), itemW));
                flowDilemmaChoices.Controls.Add(CreateRiskCard("💸 Şirket İflas & Nakit Sıkışması Riski", riskBankrupt, Color.FromArgb(155, 89, 182), itemW));
                flowDilemmaChoices.Controls.Add(CreateRiskCard("💥 Ocak İçi Grizu & Kaza Riski", riskDisaster, Color.FromArgb(192, 57, 43), itemW));

                // Subtitle 2: Proactive Actions
                Label lblRiskSub2 = new Label();
                lblRiskSub2.Text = "⚡ PROAKTİF ÖNLEMLER (Krizi Başlamadan Söndür):";
                lblRiskSub2.Font = new Font("Segoe UI", 9.2f, FontStyle.Bold);
                lblRiskSub2.ForeColor = Color.FromArgb(46, 204, 113);
                lblRiskSub2.Size = new Size(itemW, 24);
                lblRiskSub2.Margin = new Padding(0, 10, 0, 0);
                flowDilemmaChoices.Controls.Add(lblRiskSub2);

                Button btnAudit = CreateProactiveButton("🛡️ Rutin Dışı Çevre Denetimi Talep Et (-₺3,500 | Bakanlık Riskini Düşür, Halk +%6)", itemW, delegate
                {
                    string res = model.ActionProactiveEnvironmentalAudit();
                    ShowNotification(res, Color.FromArgb(46, 204, 113));
                    AddActionLog(res);
                    RefreshDilemmasPanel();
                    UpdateHudLabels();
                });
                flowDilemmaChoices.Controls.Add(btnAudit);

                Button btnUnion = CreateProactiveButton("🤝 Sendika Temsilcileriyle Toplu Sözleşme Görüşmesi (-₺4,200 | Grev Riskini Sıfırla, İSG +%14)", itemW, delegate
                {
                    string res = model.ActionProactiveUnionBargaining();
                    ShowNotification(res, Color.FromArgb(52, 152, 219));
                    AddActionLog(res);
                    RefreshDilemmasPanel();
                    UpdateHudLabels();
                });
                flowDilemmaChoices.Controls.Add(btnUnion);

                Button btnDrill = CreateProactiveButton("🚨 Grizu & Acil Durum Tahliye Tatbikatı Düzenle (-₺2,000 | Ocak İSG +%12)", itemW, delegate
                {
                    string res = model.ActionProactiveEmergencyDrill();
                    ShowNotification(res, Color.FromArgb(241, 196, 15));
                    AddActionLog(res);
                    RefreshDilemmasPanel();
                    UpdateHudLabels();
                });
                flowDilemmaChoices.Controls.Add(btnDrill);

                lblRiskAnalysis.Text = "HUKUK NOTU: Tehdit göstergeleri %60'ı aştığında ani bakanlık teftişleri ve grevler tetiklenir. Önleyici eylemler şirketi korur.";
            }

            flowDilemmaChoices.ResumeLayout(true);

            // Populate Archive ListBox
            listPastDilemmas.Items.Clear();
            if (model.PastResolvedDilemmas.Count == 0)
            {
                listPastDilemmas.Items.Add("Henüz arşivlenmiş geçmiş kriz bulunmuyor.");
            }
            else
            {
                for (int i = model.PastResolvedDilemmas.Count - 1; i >= 0; i--)
                {
                    DilemmaEvent past = model.PastResolvedDilemmas[i];
                    listPastDilemmas.Items.Add(string.Format("[Ay {0}] {1} -> Seçim: {2}",
                        past.TriggeredMonth, past.Title, past.SelectedChoiceKey));
                }
            }
        }

        private void CheckAndShowCrisisModal()
        {
            if (model == null || model.CurrentActiveDilemma == null || !model.DilemmaPendingPopup) return;

            model.DilemmaPendingPopup = false;
            using (CrisisDialogForm dlg = new CrisisDialogForm(model.CurrentActiveDilemma))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string title = model.CurrentActiveDilemma.Title;
                    model.ApplyDilemmaChoiceByKey(dlg.SelectedChoiceKey);
                    ShowNotification("Karar Uygulandı: " + title, Color.FromArgb(46, 204, 113));
                    UpdateHudLabels();
                    RefreshDilemmasPanel();
                }
            }
        }

        // ==========================================
        // TAB 4: CLIMATE HUB & TRENDS
        // ==========================================
        private void BuildClimateHubTab()
        {
            panelClimateHub = new Panel();
            panelClimateHub.Dock = DockStyle.Fill;
            panelClimateHub.BackColor = Color.FromArgb(24, 26, 30);
            panelClimateHub.AutoScroll = false;

            TableLayoutPanel tblClimate = new TableLayoutPanel();
            tblClimate.Dock = DockStyle.Fill;
            tblClimate.RowCount = 1;
            tblClimate.ColumnCount = 2;
            tblClimate.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            tblClimate.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            tblClimate.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            tblClimate.Padding = new Padding(2);
            tblClimate.Margin = new Padding(0);

            // Left Box: Science Card & Ecological Indicators
            climateLeftBox = new Panel();
            climateLeftBox.Dock = DockStyle.Fill;
            climateLeftBox.Margin = new Padding(4);
            climateLeftBox.BackColor = Color.FromArgb(30, 33, 40);
            climateLeftBox.Padding = new Padding(12);

            Label lblCardBadge = new Label();
            lblCardBadge.Text = "🌿 BİLİMSEL İKLİM BİLGİ KARTI & FARKINDALIK";
            lblCardBadge.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblCardBadge.ForeColor = Color.FromArgb(46, 204, 113);
            lblCardBadge.Location = new Point(12, 10);
            lblCardBadge.AutoSize = true;
            climateLeftBox.Controls.Add(lblCardBadge);

            lblFactTitle = new Label();
            lblFactTitle.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
            lblFactTitle.ForeColor = Color.FromArgb(241, 196, 15);
            lblFactTitle.Location = new Point(12, 28);
            lblFactTitle.Size = new Size(505, 24);
            lblFactTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            climateLeftBox.Controls.Add(lblFactTitle);

            lblFactBody = new Label();
            lblFactBody.Font = new Font("Segoe UI", 8.8f, FontStyle.Regular);
            lblFactBody.ForeColor = Color.FromArgb(210, 215, 225);
            lblFactBody.Location = new Point(12, 52);
            lblFactBody.Size = new Size(505, 56);
            lblFactBody.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            climateLeftBox.Controls.Add(lblFactBody);

            lblFactSource = new Label();
            lblFactSource.Font = new Font("Segoe UI", 7.5f, FontStyle.Italic);
            lblFactSource.ForeColor = Color.FromArgb(140, 145, 155);
            lblFactSource.Location = new Point(12, 110);
            lblFactSource.Size = new Size(505, 16);
            lblFactSource.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            climateLeftBox.Controls.Add(lblFactSource);

            lblFactAction = new Label();
            lblFactAction.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            lblFactAction.ForeColor = Color.FromArgb(100, 220, 255);
            lblFactAction.Location = new Point(12, 126);
            lblFactAction.Size = new Size(505, 20);
            lblFactAction.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            climateLeftBox.Controls.Add(lblFactAction);

            btnPrevFact = new Button();
            btnPrevFact.Text = "⬅️ Önceki Kart";
            btnPrevFact.Size = new Size(130, 26);
            btnPrevFact.Location = new Point(12, 150);
            btnPrevFact.FlatStyle = FlatStyle.Flat;
            btnPrevFact.BackColor = Color.FromArgb(35, 38, 45);
            btnPrevFact.ForeColor = Color.White;
            btnPrevFact.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            btnPrevFact.Cursor = Cursors.Hand;
            btnPrevFact.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                if (model != null)
                {
                    ShowClimateFact(model.CurrentFactIndex - 1);
                    SoundEngine.PlayClick();
                }
            });
            climateLeftBox.Controls.Add(btnPrevFact);

            btnNextFact = new Button();
            btnNextFact.Text = "Sonraki İklim Kartı ➡️";
            btnNextFact.Size = new Size(150, 26);
            btnNextFact.Location = new Point(150, 150);
            btnNextFact.FlatStyle = FlatStyle.Flat;
            btnNextFact.BackColor = Color.FromArgb(35, 38, 45);
            btnNextFact.ForeColor = Color.White;
            btnNextFact.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
            btnNextFact.Cursor = Cursors.Hand;
            btnNextFact.Click += new EventHandler(delegate(object s, EventArgs ev)
            {
                if (model != null)
                {
                    ShowClimateFact(model.CurrentFactIndex + 1);
                    SoundEngine.PlayClick();
                }
            });
            climateLeftBox.Controls.Add(btnNextFact);

            // Ecological & Climate Indicators
            lblWarmingMeter = new Label();
            lblWarmingMeter.Font = new Font("Segoe UI", 8.2f, FontStyle.Regular);
            lblWarmingMeter.ForeColor = Color.FromArgb(241, 196, 15);
            lblWarmingMeter.Location = new Point(12, 180);
            lblWarmingMeter.Size = new Size(505, 36);
            lblWarmingMeter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            climateLeftBox.Controls.Add(lblWarmingMeter);

            lblAirQualityBadge = new Label();
            lblAirQualityBadge.Font = new Font("Segoe UI", 8.2f, FontStyle.Regular);
            lblAirQualityBadge.ForeColor = Color.FromArgb(130, 220, 180);
            lblAirQualityBadge.Location = new Point(12, 218);
            lblAirQualityBadge.Size = new Size(505, 36);
            lblAirQualityBadge.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            climateLeftBox.Controls.Add(lblAirQualityBadge);

            lblSinkForestStats = new Label();
            lblSinkForestStats.Font = new Font("Segoe UI", 8.2f, FontStyle.Regular);
            lblSinkForestStats.ForeColor = Color.FromArgb(180, 225, 170);
            lblSinkForestStats.Location = new Point(12, 256);
            lblSinkForestStats.Size = new Size(505, 36);
            lblSinkForestStats.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            climateLeftBox.Controls.Add(lblSinkForestStats);

            lblEcoTracker = new Label();
            lblEcoTracker.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
            lblEcoTracker.ForeColor = Color.FromArgb(190, 205, 220);
            lblEcoTracker.Location = new Point(12, 294);
            lblEcoTracker.Size = new Size(505, 36);
            lblEcoTracker.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            climateLeftBox.Controls.Add(lblEcoTracker);

            tblClimate.Controls.Add(climateLeftBox, 0, 0);

            // Right Box: Dynamic Multi-Line Trend Chart
            chartPanel = new DoubleBufferedPanel();
            chartPanel.Dock = DockStyle.Fill;
            chartPanel.Margin = new Padding(4);
            chartPanel.BackColor = Color.FromArgb(30, 33, 40);
            chartPanel.Paint += new PaintEventHandler(ChartPanel_Paint);
            tblClimate.Controls.Add(chartPanel, 1, 0);

            panelClimateHub.Controls.Add(tblClimate);
            contentPanel.Controls.Add(panelClimateHub);
        }

        private void RefreshClimateHub()
        {
            if (model == null) return;
            ShowClimateFact(model.CurrentFactIndex);

            if (lblWarmingMeter != null)
            {
                string zone = model.GlobalWarmingTemp < 1.30 ? "🟢 Güvenli Eşik (1.5°C Altı)" :
                              model.GlobalWarmingTemp < 1.50 ? "🟡 Dikkat: Kritik Eşik Yaklaşıyor!" :
                              "🔴 Alarm: Geri Dönüşsüz İklim Tehlikesi!";
                lblWarmingMeter.Text = string.Format(
                    "🌡️ KÜRESEL ISINMA VE KRİTİK EŞİK İBRESİ:\n" +
                    "• Sıcaklık Artışı: +{0:0.00}°C | Durum: {1}\n" +
                    "• Paris 1.5°C Sınırına Kalan Emniyet Payı: +{2:0.00}°C",
                    model.GlobalWarmingTemp, zone, Math.Max(0, 1.50 - model.GlobalWarmingTemp)
                );
            }

            if (lblAirQualityBadge != null)
            {
                string aqi = model.NetCarbonEmissions < 250 ? "🟢 İyi (0-50 AQI - Temiz Vadi)" :
                             model.NetCarbonEmissions < 600 ? "🟡 Orta (51-100 AQI - Kabul Edilebilir)" :
                             model.NetCarbonEmissions < 900 ? "🟠 Hassas Gruplar İçin Zararlı" :
                             "🔴 Alarm: Tehlikeli & Asit Dumanı Riski!";
                lblAirQualityBadge.Text = string.Format(
                    "💨 BÖLGESEL HAVA KALİTESİ İNDEKSİ (AQI):\n" +
                    "• Bölgesel Hava: {0}\n" +
                    "• Filtreleme Oranı: %{1:0} | Karbon Yakalama (CCS): %{2:0}",
                    aqi, model.FilterEfficiency * 100, model.CcsEfficiency * 100
                );
            }

            if (lblSinkForestStats != null)
            {
                lblSinkForestStats.Text = string.Format(
                    "🌲 DOĞAL YUTAK & EKOSİSTEM DESTEĞİ:\n" +
                    "• Yeşil Orman Emilim Hızı: {0:0} ton CO₂/ay\n" +
                    "• Dikilen Toplam Ağaç: {1:N0} adet | Eko-Müdahale: {2} kez",
                    model.ForestAbsorptionRate, model.TotalTreesPlantedByAction, model.TotalEcoActionsCount
                );
            }

            if (lblEcoTracker != null)
            {
                lblEcoTracker.Text = string.Format(
                    "📊 GENEL İKLİM BİLANÇOSU:\n" +
                    "• Kümülatif Karbon Salımı: {0:N0} ton | En Düşük Net Salım Rekoru: {1:0} ton CO₂",
                    model.TotalEmittedCarbon, model.LowestNetCarbonRecord);
            }
        }

        private void ShowClimateFact(int index)
        {
            if (model == null || model.ClimateFacts.Count == 0) return;
            model.CurrentFactIndex = (index + model.ClimateFacts.Count) % model.ClimateFacts.Count;
            ClimateFact f = model.ClimateFacts[model.CurrentFactIndex];

            lblFactTitle.Text = f.Title;
            lblFactBody.Text = f.Body;
            lblFactSource.Text = "Kaynak: " + f.Source;
            lblFactAction.Text = "Eylem Çağrısı: " + f.ActionCall;
        }

        private void ChartPanel_Paint(object sender, PaintEventArgs e)
        {
            if (model == null) return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = chartPanel.Width;
            int h = chartPanel.Height;

            using (Font titleFont = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.FromArgb(200, 205, 215)))
            {
                g.DrawString("📈 CANLI EKOLOJİK & EKONOMİK İZLEME GRAFİĞİ", titleFont, titleBrush, 14, 10);
            }

            double renewShare = model.TotalEnergyProduced > 0 ? (model.RenewableEnergyOutput / model.TotalEnergyProduced) * 100.0 : 0;
            using (Font legFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            {
                using (SolidBrush b1 = new SolidBrush(Color.FromArgb(231, 76, 60)))
                {
                    g.DrawString(string.Format("■ Net Karbon: {0:0} t", model.NetCarbonEmissions), legFont, b1, 14, 30);
                }
                using (SolidBrush b2 = new SolidBrush(Color.FromArgb(46, 204, 113)))
                {
                    g.DrawString(string.Format("■ Yenilenebilir: %{0:0}", renewShare), legFont, b2, 170, 30);
                }
                using (SolidBrush b3 = new SolidBrush(Color.FromArgb(241, 196, 15)))
                {
                    g.DrawString(string.Format("■ Sıcaklık: +{0:0.00}°C", model.GlobalWarmingTemp), legFont, b3, 330, 30);
                }
                using (SolidBrush b4 = new SolidBrush(Color.FromArgb(155, 89, 182)))
                {
                    g.DrawString(string.Format("■ Vergi: ₺{0:N0}", model.MonthlyCarbonTax), legFont, b4, 490, 30);
                }
            }

            int plotX = 40;
            int plotY = 52;
            int plotW = w - 60;
            int plotH = h - 72;

            using (SolidBrush plotBg = new SolidBrush(Color.FromArgb(20, 22, 26)))
            using (Pen gridPen = new Pen(Color.FromArgb(35, 38, 45), 1f))
            {
                g.FillRectangle(plotBg, plotX, plotY, plotW, plotH);
                g.DrawRectangle(gridPen, plotX, plotY, plotW, plotH);
                for (int i = 1; i < 4; i++)
                {
                    int gy = plotY + (plotH / 4) * i;
                    g.DrawLine(gridPen, plotX, gy, plotX + plotW, gy);
                }
            }

            if (model.CarbonHistory.Count < 2)
            {
                using (Font noDataFont = new Font("Segoe UI", 9f, FontStyle.Italic))
                using (SolidBrush noDataBrush = new SolidBrush(Color.Gray))
                {
                    g.DrawString("Veriler toplanıyor... (Birkaç ay ilerleyin)", noDataFont, noDataBrush, plotX + 110, plotY + 60);
                }
                return;
            }

            int count = model.CarbonHistory.Count;
            float stepX = (float)plotW / Math.Max(1, count - 1);

            // 1. Carbon line (Red)
            using (Pen carbonPen = new Pen(Color.FromArgb(231, 76, 60), 2.2f))
            using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(231, 76, 60)))
            {
                for (int i = 0; i < count - 1; i++)
                {
                    float x1 = plotX + i * stepX;
                    float y1 = plotY + plotH - (float)(Math.Min(1200.0, model.CarbonHistory[i]) / 1200.0 * plotH);
                    float x2 = plotX + (i + 1) * stepX;
                    float y2 = plotY + plotH - (float)(Math.Min(1200.0, model.CarbonHistory[i + 1]) / 1200.0 * plotH);
                    g.DrawLine(carbonPen, x1, y1, x2, y2);
                    g.FillEllipse(dotBrush, x1 - 2.5f, y1 - 2.5f, 5f, 5f);
                }
            }

            // 2. Renewable % line (Green)
            using (Pen renewPen = new Pen(Color.FromArgb(46, 204, 113), 2.2f))
            using (SolidBrush dotBrush = new SolidBrush(Color.FromArgb(46, 204, 113)))
            {
                for (int i = 0; i < count - 1; i++)
                {
                    float x1 = plotX + i * stepX;
                    float y1 = plotY + plotH - (float)(model.RenewablePercentHistory[i] / 100.0 * plotH);
                    float x2 = plotX + (i + 1) * stepX;
                    float y2 = plotY + plotH - (float)(model.RenewablePercentHistory[i + 1] / 100.0 * plotH);
                    g.DrawLine(renewPen, x1, y1, x2, y2);
                    g.FillEllipse(dotBrush, x1 - 2.5f, y1 - 2.5f, 5f, 5f);
                }
            }

            // 3. Temp line (Yellow)
            using (Pen tempPen = new Pen(Color.FromArgb(241, 196, 15), 1.8f))
            {
                for (int i = 0; i < count - 1 && i < model.TempHistory.Count - 1; i++)
                {
                    float x1 = plotX + i * stepX;
                    float y1 = plotY + plotH - (float)((model.TempHistory[i] - 1.0) / 1.5 * plotH);
                    float x2 = plotX + (i + 1) * stepX;
                    float y2 = plotY + plotH - (float)((model.TempHistory[i + 1] - 1.0) / 1.5 * plotH);
                    y1 = Math.Max(plotY, Math.Min(plotY + plotH, y1));
                    y2 = Math.Max(plotY, Math.Min(plotY + plotH, y2));
                    g.DrawLine(tempPen, x1, y1, x2, y2);
                }
            }

            // 4. Carbon Tax line (Purple)
            using (Pen taxPen = new Pen(Color.FromArgb(155, 89, 182), 1.8f))
            {
                for (int i = 0; i < count - 1 && i < model.CarbonTaxHistory.Count - 1; i++)
                {
                    float x1 = plotX + i * stepX;
                    float y1 = plotY + plotH - (float)(Math.Min(25000.0, model.CarbonTaxHistory[i]) / 25000.0 * plotH);
                    float x2 = plotX + (i + 1) * stepX;
                    float y2 = plotY + plotH - (float)(Math.Min(25000.0, model.CarbonTaxHistory[i + 1]) / 25000.0 * plotH);
                    g.DrawLine(taxPen, x1, y1, x2, y2);
                }
            }
        }

        // ==========================================
        // 5. ACHIEVEMENTS / KUPA ODASI TAB
        // ==========================================
        private void BuildAchievementsTab()
        {
            panelAchievements = new Panel();
            panelAchievements.Dock = DockStyle.Fill;
            panelAchievements.BackColor = Color.FromArgb(24, 26, 30);
            panelAchievements.Visible = false;

            Panel topCard = new Panel();
            topCard.Dock = DockStyle.Top;
            topCard.Height = 58;
            topCard.BackColor = Color.FromArgb(20, 23, 29);
            topCard.Padding = new Padding(12, 8, 12, 8);

            Label lblTitle = new Label();
            lblTitle.Text = "🏆 MÜDÜRLÜK KUPA ODASI & PRESTİJ MADALYALARI";
            lblTitle.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(241, 196, 15);
            lblTitle.Location = new Point(12, 8);
            lblTitle.AutoSize = true;
            topCard.Controls.Add(lblTitle);

            lblAchievementsSummary = new Label();
            lblAchievementsSummary.Text = "0 / 10 Madalya Kazanıldı | Görevdeki her büyük çevre ve finans başarısı kalıcı prim kazandırır!";
            lblAchievementsSummary.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            lblAchievementsSummary.ForeColor = Color.FromArgb(170, 180, 195);
            lblAchievementsSummary.Location = new Point(14, 32);
            lblAchievementsSummary.AutoSize = true;
            topCard.Controls.Add(lblAchievementsSummary);

            panelAchievements.Controls.Add(topCard);

            flowAchievements = new FlowLayoutPanel();
            flowAchievements.Dock = DockStyle.Fill;
            flowAchievements.AutoScroll = true;
            flowAchievements.BackColor = Color.FromArgb(24, 26, 30);
            flowAchievements.Padding = new Padding(8);
            panelAchievements.Controls.Add(flowAchievements);

            contentPanel.Controls.Add(panelAchievements);
        }

        private void RefreshAchievementsPanel()
        {
            if (model == null || flowAchievements == null) return;

            int unlockedCount = model.GetUnlockedAchievementsCount();
            lblAchievementsSummary.Text = string.Format(
                "{0} / {1} Prestij Madalyası Kazanıldı | Toplam Ekstra Fon Bonusu: ₺{2:N0}",
                unlockedCount, model.Achievements.Count, unlockedCount * 6500
            );

            flowAchievements.SuspendLayout();
            flowAchievements.Controls.Clear();

            int availableW = Math.Max(500, flowAchievements.ClientSize.Width - 36);
            int cardW = availableW > 900 ? (availableW / 2) : availableW;
            int cardH = 82;

            for (int i = 0; i < model.Achievements.Count; i++)
            {
                AchievementItem ach = model.Achievements[i];
                Panel card = new Panel();
                card.Size = new Size(cardW, cardH);
                card.Margin = new Padding(6);
                card.BackColor = ach.Unlocked ? Color.FromArgb(28, 42, 36) : Color.FromArgb(22, 25, 32);

                Label lblIcon = new Label();
                lblIcon.Text = ach.Icon;
                lblIcon.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
                lblIcon.Size = new Size(48, 48);
                lblIcon.Location = new Point(12, 16);
                lblIcon.TextAlign = ContentAlignment.MiddleCenter;
                card.Controls.Add(lblIcon);

                Label lblT = new Label();
                lblT.Text = ach.Title;
                lblT.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                lblT.ForeColor = ach.Unlocked ? Color.FromArgb(241, 196, 15) : Color.FromArgb(215, 220, 230);
                lblT.Location = new Point(68, 10);
                lblT.AutoSize = true;
                card.Controls.Add(lblT);

                Label lblD = new Label();
                lblD.Text = ach.Description;
                lblD.Font = new Font("Segoe UI", 8.2f, FontStyle.Regular);
                lblD.ForeColor = Color.FromArgb(150, 160, 175);
                lblD.Location = new Point(69, 32);
                lblD.Size = new Size(Math.Max(200, cardW - 160), 20);
                card.Controls.Add(lblD);

                if (ach.Unlocked)
                {
                    Label lblStatus = new Label();
                    lblStatus.Text = string.Format("⭐ KAZANILDI (Ay {0})\n+₺{1:N0} Teşvik Alındı", ach.UnlockedMonth, ach.CashBonus);
                    lblStatus.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                    lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                    lblStatus.Location = new Point(cardW - 145, 12);
                    lblStatus.Size = new Size(135, 36);
                    lblStatus.TextAlign = ContentAlignment.TopRight;
                    card.Controls.Add(lblStatus);
                }
                else
                {
                    int pct = ach.GetProgressPercent();
                    Label lblProgText = new Label();
                    lblProgText.Text = string.Format("{0:N0} / {1:N0} {2} (%{3})", ach.CurrentProgress, ach.TargetProgress, ach.Unit, pct);
                    lblProgText.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
                    lblProgText.ForeColor = Color.FromArgb(200, 205, 215);
                    lblProgText.Location = new Point(69, 56);
                    lblProgText.AutoSize = true;
                    card.Controls.Add(lblProgText);

                    int barW = Math.Max(80, cardW - 350);
                    Panel progBarBg = new Panel();
                    progBarBg.Location = new Point(220, 58);
                    progBarBg.Size = new Size(barW, 10);
                    progBarBg.BackColor = Color.FromArgb(40, 45, 55);

                    Panel progBarFill = new Panel();
                    progBarFill.Location = new Point(0, 0);
                    progBarFill.Size = new Size((int)(barW * (pct / 100.0)), 10);
                    progBarFill.BackColor = Color.FromArgb(52, 152, 219);
                    progBarBg.Controls.Add(progBarFill);

                    card.Controls.Add(progBarBg);

                    Label lblBonus = new Label();
                    lblBonus.Text = string.Format("+₺{0:N0} Ödül", ach.CashBonus);
                    lblBonus.Font = new Font("Segoe UI", 8f, FontStyle.Italic);
                    lblBonus.ForeColor = Color.FromArgb(241, 196, 15);
                    lblBonus.Location = new Point(cardW - 115, 54);
                    lblBonus.AutoSize = true;
                    card.Controls.Add(lblBonus);
                }

                card.Paint += new PaintEventHandler(delegate(object s, PaintEventArgs ev)
                {
                    Panel p = s as Panel;
                    Color bc = ach.Unlocked ? Color.FromArgb(241, 196, 15) : Color.FromArgb(45, 50, 62);
                    using (Pen pBorder = new Pen(bc, ach.Unlocked ? 1.5f : 1f))
                    {
                        ev.Graphics.DrawRectangle(pBorder, 0, 0, p.Width - 1, p.Height - 1);
                    }
                });

                flowAchievements.Controls.Add(card);
            }

            flowAchievements.ResumeLayout();
        }

        // ==========================================
        // GAME LOOPS & TIMERS
        // ==========================================
        private void StartRenderLoop()
        {
            renderTimer = new Timer();
            renderTimer.Interval = 20;
            renderTimer.Tick += new EventHandler(RenderTimer_Tick);
            renderTimer.Start();

            monthTimer = new Timer();
            monthTimer.Interval = 3000;
            monthTimer.Tick += new EventHandler(MonthTimer_Tick);

            ambientMusicTimer = new Timer();
            ambientMusicTimer.Interval = 16000;
            ambientMusicTimer.Tick += new EventHandler(delegate(object s, EventArgs ev)
            {
                if (model != null && !model.IsPaused && currentScreen == GameScreen.Playing)
                {
                    SoundEngine.PlayAmbientChord(ambientPhraseIndex++);
                }
            });
            ambientMusicTimer.Start();
        }

        private void RenderTimer_Tick(object sender, EventArgs e)
        {
            menuAnimPhase += 0.016f;

            if (currentScreen == GameScreen.MainMenu)
            {
                menuPanel.Invalidate();
                return;
            }

            if (currentScreen == GameScreen.Playing && model != null && renderer != null && !model.IsPaused)
            {
                renderer.Update(model);
                canvasPanel.Invalidate();
            }
        }

        private void MonthTimer_Tick(object sender, EventArgs e)
        {
            if (model == null || model.IsPaused || model.GameOver) return;
            AdvanceOneMonth();
        }

        private void BtnNextMonth_Click(object sender, EventArgs e)
        {
            if (model == null || model.GameOver) return;
            AdvanceOneMonth();
            SoundEngine.PlayClick();
        }

        private void AdvanceOneMonth()
        {
            model.NextMonthTick();
            UpdateHudLabels();
            UpdateNewsTicker();

            if (activeTab == "upgrades") RefreshUpgradesPanel();
            if (activeTab == "dilemmas") RefreshDilemmasPanel();
            if (activeTab == "climate")
            {
                RefreshClimateHub();
                if (chartPanel != null) chartPanel.Invalidate();
            }
            if (activeTab == "achievements") RefreshAchievementsPanel();

            if (model.LastUnlockedAchievement != null)
            {
                ShowNotification(
                    string.Format("🏆 BAŞARI KAZANILDI: {0}! (+₺{1:N0} Teşvik)", model.LastUnlockedAchievement.Title, model.LastUnlockedAchievement.CashBonus),
                    Color.FromArgb(241, 196, 15)
                );
                model.LastUnlockedAchievement = null;
            }

            CheckAndShowCrisisModal();

            if (model.GameOver)
            {
                monthTimer.Stop();
                ShowEndGameReport();
            }
        }

        private void CanvasPanel_Paint(object sender, PaintEventArgs e)
        {
            if (renderer != null && model != null)
            {
                renderer.Render(e.Graphics, canvasPanel.Width, canvasPanel.Height, model);
            }
        }

        private void CanvasPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && renderer != null && model != null)
            {
                if (renderer.TryClickParticle(e.Location, model))
                {
                    UpdateHudLabels();
                    canvasPanel.Invalidate();
                }
            }
        }

        // ==========================================
        // HUD UPDATES
        // ==========================================
        private void UpdateHudLabels()
        {
            if (model == null) return;

            lblDate.Text = string.Format("Ay {0}/60 ({1})\n{2}", model.CurrentMonth, model.GetCurrentDateString(), model.GetSeasonName());

            double netFlow = model.MonthlyRevenue - model.MonthlyExpenses;
            lblBudget.Text = string.Format("₺{0:N0}\n({1}₺{2:N0}/ay)", model.Budget, netFlow >= 0 ? "+" : "", netFlow);
            lblBudget.ForeColor = model.Budget >= 0 ? Color.FromArgb(46, 204, 113) : Color.FromArgb(231, 76, 60);

            lblEnergy.Text = string.Format("{0:0} / {1:0}\nMWh ({2:0} Temiz)", model.TotalEnergyProduced, model.EnergyDemand, model.RenewableEnergyOutput);
            lblEnergy.ForeColor = model.TotalEnergyProduced >= model.EnergyDemand ? Color.FromArgb(241, 196, 15) : Color.FromArgb(231, 76, 60);

            lblCarbon.Text = string.Format("{0:0} t CO₂/ay\n(Vergi: ₺{1:N0})", model.NetCarbonEmissions, model.MonthlyCarbonTax);
            if (model.NetCarbonEmissions < 250) lblCarbon.ForeColor = Color.FromArgb(46, 204, 113);
            else if (model.NetCarbonEmissions < 650) lblCarbon.ForeColor = Color.FromArgb(241, 196, 15);
            else lblCarbon.ForeColor = Color.FromArgb(231, 76, 60);

            lblThreat.Text = string.Format("%{0:0}\n(+{1:0.00}°C)", model.ClimateThreatPercent, model.GlobalWarmingTemp);
            if (model.ClimateThreatPercent < 35) lblThreat.ForeColor = Color.FromArgb(46, 204, 113);
            else if (model.ClimateThreatPercent < 70) lblThreat.ForeColor = Color.FromArgb(243, 156, 18);
            else lblThreat.ForeColor = Color.FromArgb(231, 76, 60);

            lblApproval.Text = string.Format("%{0:0}\n(Güvenlik: %{1:0})", model.PublicApproval, model.MineSafety);
            lblApproval.ForeColor = model.PublicApproval >= 50 ? Color.FromArgb(52, 152, 219) : Color.FromArgb(231, 76, 60);

            // Tab 1 details
            lblCoalStockBar.Text = string.Format("📦 Kömür Rezervi: {0:0} Ton (Tükenirse santral yavaşlar!)", model.RawCoalStockTons);
            lblStaminaBar.Text = string.Format("👷 İşçi Kondisyonu: %{0:0} (Düşükse vardiya yapılamaz)", model.WorkerStamina);

            lblStatsDetails.Text = string.Format(
                "• Fosil Kömür Üretimi: {0:0} MWh | Temiz Güç: {1:0} MWh\n" +
                "• Filtreleme & CCS: -%{2:0} | Yeşil Yutak: -{3:0} t CO₂\n" +
                "• Net Karbon Vergisi Yükü: -₺{4:N0} / ay\n" +
                "• Aylık Sabit Masraflar: -₺{5:N0} (İşçi maaşları & bakım)",
                model.CoalEnergyOutput, model.RenewableEnergyOutput,
                (model.FilterEfficiency + model.CcsEfficiency) * 100,
                model.ForestAbsorptionRate, model.MonthlyCarbonTax,
                model.BaseOperationalExpense
            );

            // Contracts panel update
            string munStatus = model.TotalEnergyProduced >= 450.0 ? "✅ TESLİM EDİLİYOR" : "❌ EKSİK! (Ceza Riski)";
            string indStatus = model.TotalEnergyProduced >= 750.0 ? "✅ TESLİM EDİLİYOR" : "⚠️ KISMİ / EKSİK";

            lblContractsInfo.Text = string.Format(
                "1. BELEDİYE SÖZLEŞMESİ (450 MWh / ₺52,000):\n   Durum: {0}\n\n" +
                "2. SANAYİ BÖLGESİ (300 MWh / ₺40,000):\n   Durum: {1}\n\n" +
                "3. SPOT PİYASA FİYATI: ₺{2:0.0} / MWh\n" +
                "• Bu ay kesilen sözleşme cezası: -₺{3:N0}",
                munStatus, indStatus, model.SpotEnergyPricePerMWh, model.ContractPenaltyIncurred
            );

            if (lblTelemetryInfo != null)
            {
                double renewPercent = model.TotalEnergyProduced > 0 ? (model.RenewableEnergyOutput / model.TotalEnergyProduced) * 100.0 : 0;
                double burnRate = (model.CoalEnergyOutput / model.CoalCapacityMax) * 35.0;
                double monthsLeft = burnRate > 0 ? model.RawCoalStockTons / burnRate : 999;
                string grade = renewPercent > 60 ? "A+ (Mükemmel Yeşil)" : renewPercent > 35 ? "B (İyi Dönüşüm)" : renewPercent > 15 ? "C (Orta)" : "D (Ağır Fosil)";

                string advice;
                if (model.RawCoalStockTons < 30)
                    advice = "⚠️ Kömür stoku tükenmek üzere! Maden vardiyası başlatın veya temiz enerjiyi artırın.";
                else if (model.NetCarbonEmissions > 800)
                    advice = "⚠️ Karbon vergisi çok yüksek! Baca kimyasal yıkaması yapın veya fidan dikin.";
                else if (renewPercent >= 50)
                    advice = "🌱 Şebekenin yarısından fazlası yeşil! Paris hedefleri yolunda.";
                else
                    advice = "💡 Dengeli üretim sürüyor. Fazla enerjiyi spot piyasada anında satabilirsiniz.";

                lblTelemetryInfo.Text = string.Format(
                    "📊 CANLI TELEMETRİ & BAŞ MÜHENDİS RAPORU:\n" +
                    "• Temiz Enerji Payı: %{0:0.0} (Verimlilik Notu: {1})\n" +
                    "• Kömür Tüketim Hızı: ~{2:0} t/ay (Kalan Rezerv: ~{3:0.0} ay)\n" +
                    "• Baş Mühendis Tavsiyesi: {4}",
                    renewPercent, grade, burnRate, monthsLeft, advice
                );
            }

            if (lblContractCommitmentDetails != null)
            {
                double munPct = Math.Min(100.0, (model.TotalEnergyProduced / 450.0) * 100.0);
                double indPct = Math.Min(100.0, (model.TotalEnergyProduced / 750.0) * 100.0);
                lblContractCommitmentDetails.Text = string.Format(
                    "📊 ŞEBEKE TESLİMAT YÜZDELERİ:\n" +
                    "• Belediye (450 MWh): %{0:0} Karşılandı {1}\n" +
                    "• Sanayi (300 MWh): %{2:0} Karşılandı {3}\n" +
                    "• Ceza Durumu: {4}",
                    munPct, munPct >= 100 ? "✅" : "⚠️",
                    indPct, indPct >= 100 ? "✅" : "⚠️",
                    model.ContractPenaltyIncurred > 0 ? string.Format("₺{0:N0} kesildi", model.ContractPenaltyIncurred) : "₺0 (Güvende)"
                );
            }

            if (lblMarketSpotBourse != null)
            {
                lblMarketSpotBourse.Text = string.Format(
                    "⚡ CANLI ENERJİ BORSASI (EPİAŞ / SPOT):\n" +
                    "• Güncel Piyasa Fiyatı: ₺{0:0.0} / MWh\n" +
                    "• Talep Durumu: {1}\n" +
                    "• İpucu: Spot piyasada manuel satış yaparak kasayı hızla büyütebilirsiniz.",
                    model.SpotEnergyPricePerMWh,
                    model.TotalEnergyProduced >= model.EnergyDemand ? "🟢 Dengeli / Arz Fazlası" : "🔴 Yüksek Talep / Açık Var"
                );
            }
        }

        private void UpdateNewsTicker()
        {
            if (model == null) return;

            string news;
            if (model.ClimateThreatPercent > 70)
            {
                news = "ALARM: Bölgede rekor sıcaklıklar ve asit yağmuru riski! Çevre örgütleri acil önlem çağrısı yapıyor.";
            }
            else if (model.RenewableEnergyOutput > model.EnergyDemand * 0.5)
            {
                news = "BAŞARI: Rüzgar ve güneş santralleri şehrin enerjisinin yarısını karşılayarak emisyon rekoru kırdı!";
            }
            else if (model.NetCarbonEmissions > 850)
            {
                news = "UYARI: Bacalardan yükselen duman sebebiyle ağır karbon vergisi kesildi!";
            }
            else if (model.PublicApproval > 85)
            {
                news = "ÖVGÜ: Halk, madenin çevreci yaklaşımını destekliyor. Bölge yönetimi işbirliği teklif etti.";
            }
            else
            {
                news = string.Format("GÜNCEL: Müdür {0} liderliğinde Karbon-9 tesisi temiz enerji dönüşüm hedeflerini sürdürüyor.", model.PlayerName);
            }
            lblNewsTicker.Text = news;
        }

        private void BtnSpeed_Click(object sender, EventArgs e)
        {
            if (model == null) return;

            if (model.GameSpeed == 1)
            {
                model.GameSpeed = 2;
                monthTimer.Interval = Math.Max(800, monthTimer.Interval / 2);
                btnSpeed.Text = "▶ 1x";
                btnSpeed.BackColor = Color.FromArgb(46, 204, 113);
                btnSpeed.ForeColor = Color.Black;
            }
            else
            {
                model.GameSpeed = 1;
                monthTimer.Interval = monthTimer.Interval * 2;
                btnSpeed.Text = "⏩ 2x";
                btnSpeed.BackColor = Color.FromArgb(35, 38, 45);
                btnSpeed.ForeColor = Color.White;
            }
            SoundEngine.PlayClick();
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            if (model == null) return;
            model.IsPaused = !model.IsPaused;
            if (model.IsPaused)
            {
                btnPause.Text = "▶ Devam";
                btnPause.BackColor = Color.FromArgb(241, 196, 15);
                btnPause.ForeColor = Color.Black;
            }
            else
            {
                btnPause.Text = "⏸ Dur";
                btnPause.BackColor = Color.FromArgb(35, 38, 45);
                btnPause.ForeColor = Color.White;
            }
            SoundEngine.PlayClick();
        }

        private void BtnSound_Click(object sender, EventArgs e)
        {
            SoundEngine.SoundEnabled = !SoundEngine.SoundEnabled;
            btnSound.Text = SoundEngine.SoundEnabled ? "🔊" : "🔇";
            btnSound.BackColor = SoundEngine.SoundEnabled ? Color.FromArgb(35, 38, 45) : Color.FromArgb(60, 40, 40);
            SoundEngine.PlayClick();
        }

        private void ShowEndGameReport()
        {
            if (model == null) return;

            string outcomeTitle = model.Victory ? "🏆 TEBRİKLER! " + model.VictoryTier : "⚠️ " + model.VictoryTier;
            double renewShare = model.TotalEnergyProduced > 0 ? (model.RenewableEnergyOutput / model.TotalEnergyProduced) * 100.0 : 0;

            string reportMsg = string.Format(
                "{0}\n\n" +
                "========================================\n" +
                "MADEN MÜDÜRÜ {1} - 5 YILLIK İKLİM KARNESİ\n" +
                "========================================\n" +
                "• Görev Süresi: 60 Ay (5 Tam Yıl)\n" +
                "• Başarı Rütbesi: {2}\n" +
                "• Kasa Bakiyesi: ₺{3:N0}\n" +
                "• Yenilenebilir Enerji Oranı: %{4:0.0}\n" +
                "• Son Aylık Net Karbon Salımı: {5:0} ton CO₂\n" +
                "• Toplam Atmosfere Salınan: {6:N0} ton CO₂\n" +
                "• Küresel Sıcaklık Artışı: +{7:0.00}°C\n" +
                "• Halk Desteği Endeksi: %{8:0}\n" +
                "• Manuel Temizlenen Karbon: {9:0} ton 🌿\n\n" +
                "DEĞERLENDİRME:\n{10}\n\n" +
                "-----------------------------------------\n" +
                "GERÇEK DÜNYA İKLİM ÇAĞRISI:\n" +
                "Fosil yakıtlar küresel sera gazı salımlarının en büyük kaynağıdır. " +
                "Yenilenebilir enerjiye hızlı ve kararlı geçiş, gezegenimizi 1.5°C altında tutmanın tek yoludur!\n\n" +
                "Yeniden oynamak ister misiniz?",
                outcomeTitle,
                model.PlayerName,
                model.VictoryTier,
                model.Budget,
                renewShare,
                model.NetCarbonEmissions,
                model.TotalEmittedCarbon,
                model.GlobalWarmingTemp,
                model.PublicApproval,
                model.TotalEcoScrubbedTons,
                model.GameOverReason
            );

            DialogResult res = MessageBox.Show(reportMsg, outcomeTitle, MessageBoxButtons.YesNo,
                model.Victory ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            if (res == DialogResult.Yes)
            {
                ShowScreen(GameScreen.NameInput);
            }
            else
            {
                ShowScreen(GameScreen.MainMenu);
            }
        }
    }
}
