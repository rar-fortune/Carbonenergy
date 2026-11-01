using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace CarbonEnergy
{
    // ==========================================
    // SMOKE PARTICLE
    // ==========================================
    public class SmokeParticle
    {
        public float X;
        public float Y;
        public float Vx;
        public float Vy;
        public float Radius;
        public float MaxRadius;
        public float Alpha;
        public Color BaseColor;
        public bool Alive;
        public float InitialRadius;

        public RectangleF GetBounds()
        {
            return new RectangleF(X - Radius, Y - Radius, Radius * 2, Radius * 2);
        }
    }

    // ==========================================
    // FLOATING POPUP TEXT
    // ==========================================
    public class FloatingText
    {
        public string Text;
        public float X;
        public float Y;
        public float Vy;
        public float Alpha;
        public Color TextColor;
        public bool Alive;
    }

    // ==========================================
    // WORLD RENDERER (GDI+ ANIMATED CANVAS)
    // Enhanced with multi-layer sky, mountains,
    // river, birds, improved buildings, underground,
    // conveyor belts, and environmental details.
    // ==========================================
    public class WorldRenderer
    {
        private Random rnd = new Random();
        private List<SmokeParticle> particles = new List<SmokeParticle>();
        private List<FloatingText> floatingTexts = new List<FloatingText>();

        // Animation counters
        private float turbineAngle = 0f;
        private float wheelAngle = 0f;
        private float cartPositionX = 140f;
        private float cartDirection = 1f;
        private float cloudOffset = 0f;
        private float pulsePhase = 0f;

        // New animation state
        private float riverShimmerPhase = 0f;
        private float conveyorPhase = 0f;
        private float blinkTimer = 0f;
        private float treeSway = 0f;
        private float dripPhase = 0f;
        private float birdPhase = 0f;

        // Bird positions (x, y, speed)
        private float[] birdX;
        private float[] birdY;
        private float[] birdSpeed;
        private bool birdsInitialized = false;

        // Water drip positions
        private float[] dripY;
        private float[] dripX;
        private bool dripsInitialized = false;

        // Weather particle state
        private bool weatherInitialized = false;
        private float[] snowX;
        private float[] snowY;
        private float[] snowSpeed;
        private float[] snowSize;

        private float[] leafX;
        private float[] leafY;
        private float[] leafSpeedX;
        private float[] leafSpeedY;
        private Color[] leafColors;

        public void Update(GameModel model)
        {
            // Wind turbine rotation
            float turbineSpeed = 3.5f;
            turbineAngle = (turbineAngle + turbineSpeed) % 360f;

            // Initialize weather if needed
            if (!weatherInitialized)
            {
                weatherInitialized = true;
                snowX = new float[55];
                snowY = new float[55];
                snowSpeed = new float[55];
                snowSize = new float[55];
                for (int i = 0; i < 55; i++)
                {
                    snowX[i] = (float)(rnd.NextDouble() * 1250);
                    snowY[i] = (float)(rnd.NextDouble() * 250);
                    snowSpeed[i] = (float)(rnd.NextDouble() * 1.3 + 0.8);
                    snowSize[i] = (float)(rnd.NextDouble() * 2.2 + 1.2);
                }

                leafX = new float[35];
                leafY = new float[35];
                leafSpeedX = new float[35];
                leafSpeedY = new float[35];
                leafColors = new Color[] {
                    Color.FromArgb(230, 126, 34),
                    Color.FromArgb(241, 196, 15),
                    Color.FromArgb(192, 57, 43),
                    Color.FromArgb(211, 84, 0),
                    Color.FromArgb(180, 100, 30)
                };
                for (int i = 0; i < 35; i++)
                {
                    leafX[i] = (float)(rnd.NextDouble() * 1250);
                    leafY[i] = (float)(rnd.NextDouble() * 250);
                    leafSpeedX[i] = (float)(rnd.NextDouble() * 1.6 + 0.6);
                    leafSpeedY[i] = (float)(rnd.NextDouble() * 0.9 + 0.5);
                }
            }

            int currentSeasonMonth = ((model.CurrentMonth - 1) % 12) + 1;
            // Winter snow update
            if (currentSeasonMonth == 12 || currentSeasonMonth <= 2)
            {
                for (int i = 0; i < 55; i++)
                {
                    snowY[i] += snowSpeed[i];
                    snowX[i] += (float)Math.Sin(pulsePhase * 1.5 + i) * 0.45f;
                    if (snowY[i] > 245)
                    {
                        snowY[i] = -5;
                        snowX[i] = (float)(rnd.NextDouble() * 1250);
                    }
                }
            }
            // Autumn leaf drift update
            else if (currentSeasonMonth >= 9 && currentSeasonMonth <= 11)
            {
                for (int i = 0; i < 35; i++)
                {
                    leafY[i] += leafSpeedY[i];
                    leafX[i] += leafSpeedX[i] + (float)Math.Sin(pulsePhase * 2.0 + i) * 0.5f;
                    if (leafY[i] > 245 || leafX[i] > 1250)
                    {
                        leafY[i] = -5;
                        leafX[i] = (float)(rnd.NextDouble() * 1250);
                    }
                }
            }

            // Mine headframe wheel & cart
            wheelAngle = (wheelAngle + 4.0f * (float)model.CoalProductionSlider) % 360f;

            cartPositionX += cartDirection * 1.8f * (float)model.CoalProductionSlider;
            if (cartPositionX > 320f)
            {
                cartDirection = -1f;
            }
            else if (cartPositionX < 110f)
            {
                cartDirection = 1f;
            }

            // Clouds drift
            cloudOffset = (cloudOffset + 0.35f) % 1200f;

            // Pulse phase for CCS and solar flares
            pulsePhase += 0.08f;
            if (pulsePhase > (float)(Math.PI * 2)) pulsePhase -= (float)(Math.PI * 2);

            // River shimmer
            riverShimmerPhase += 0.12f;
            if (riverShimmerPhase > (float)(Math.PI * 2)) riverShimmerPhase -= (float)(Math.PI * 2);

            // Conveyor belt animation
            conveyorPhase += 1.5f * (float)model.CoalProductionSlider;
            if (conveyorPhase > 20f) conveyorPhase -= 20f;

            // Blink timer for warning lights
            blinkTimer += 0.1f;
            if (blinkTimer > (float)(Math.PI * 2)) blinkTimer -= (float)(Math.PI * 2);

            // Tree sway
            treeSway += 0.04f;
            if (treeSway > (float)(Math.PI * 2)) treeSway -= (float)(Math.PI * 2);

            // Water drip animation
            dripPhase += 0.06f;
            if (dripPhase > (float)(Math.PI * 2)) dripPhase -= (float)(Math.PI * 2);

            // Bird flight
            birdPhase += 0.02f;
            if (!birdsInitialized)
            {
                birdsInitialized = true;
                birdX = new float[] { 100f, 350f, 600f, 800f, 950f };
                birdY = new float[] { 35f, 55f, 28f, 48f, 38f };
                birdSpeed = new float[] { 0.8f, 1.1f, 0.6f, 0.9f, 1.3f };
            }
            for (int i = 0; i < birdX.Length; i++)
            {
                birdX[i] += birdSpeed[i];
                if (birdX[i] > 1250f) birdX[i] = -30f;
            }

            // Initialize drip positions
            if (!dripsInitialized)
            {
                dripsInitialized = true;
                dripX = new float[] { 180f, 250f, 330f, 200f, 370f };
                dripY = new float[] { 0f, 0.3f, 0.6f, 0.15f, 0.75f };
            }
            for (int i = 0; i < dripY.Length; i++)
            {
                dripY[i] += 0.025f;
                if (dripY[i] > 1f) dripY[i] = 0f;
            }

            // Spawn smoke particles from chimneys
            if (!model.GameOver && rnd.Next(100) < (int)(model.CoalProductionSlider * 60))
            {
                SpawnSmokeParticle(model, 442f, 138f);
                if (model.CoalProductionSlider > 1.0)
                {
                    SpawnSmokeParticle(model, 468f, 150f);
                }
            }

            // Update smoke particles
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                SmokeParticle p = particles[i];
                p.X += p.Vx;
                p.Y += p.Vy;
                p.Radius = Math.Min(p.MaxRadius, p.Radius + 0.35f);
                p.Alpha -= 1.8f;
                if (p.Alpha <= 0 || p.Y < -50 || p.X > 1300)
                {
                    particles.RemoveAt(i);
                }
            }

            // Update floating texts
            for (int i = floatingTexts.Count - 1; i >= 0; i--)
            {
                FloatingText ft = floatingTexts[i];
                ft.Y += ft.Vy;
                ft.Alpha -= 3.5f;
                if (ft.Alpha <= 0)
                {
                    floatingTexts.RemoveAt(i);
                }
            }
        }

        private void SpawnSmokeParticle(GameModel model, float startX, float startY)
        {
            if (particles.Count > 120) return;

            Color pColor;
            if (model.CcsEfficiency > 0.4)
            {
                pColor = Color.FromArgb(235, 245, 255);
            }
            else if (model.FilterEfficiency > 0.3)
            {
                pColor = Color.FromArgb(170, 175, 180);
            }
            else
            {
                pColor = Color.FromArgb(45, 45, 48);
            }

            float initR = (float)(rnd.NextDouble() * 4.0 + 5.0);
            SmokeParticle p = new SmokeParticle
            {
                X = startX + (float)(rnd.NextDouble() * 8.0 - 4.0),
                Y = startY,
                Vx = (float)(rnd.NextDouble() * 1.5 + 0.8),
                Vy = -(float)(rnd.NextDouble() * 1.8 + 1.2),
                Radius = initR,
                InitialRadius = initR,
                MaxRadius = (float)(rnd.NextDouble() * 16.0 + 24.0),
                Alpha = 190f,
                BaseColor = pColor,
                Alive = true
            };
            particles.Add(p);
        }

        public void AddFloatingText(string text, float x, float y, Color col)
        {
            FloatingText ft = new FloatingText
            {
                Text = text,
                X = x,
                Y = y,
                Vy = -1.2f,
                Alpha = 255f,
                TextColor = col,
                Alive = true
            };
            floatingTexts.Add(ft);
        }

        public bool TryClickParticle(Point mousePt, GameModel model)
        {
            // 1. Check smoke particles
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                SmokeParticle p = particles[i];
                RectangleF bounds = p.GetBounds();
                bounds.Inflate(12f, 12f);
                if (bounds.Contains(mousePt.X, mousePt.Y))
                {
                    particles.RemoveAt(i);
                    model.ManualScrubCarbon();
                    AddFloatingText("-18 t CO2 \uD83C\uDF3F +\u20BA55", p.X, p.Y, Color.LightGreen);
                    return true;
                }
            }

            // 2. Check interactive world structures
            int x = mousePt.X;
            int y = mousePt.Y;

            // Wildlife click (Deer / Fawn)
            if (model.ForestAbsorptionRate >= 20.0 && model.ClimateThreatPercent < 65.0)
            {
                if (x >= 45 && x <= 95 && y >= 195 && y <= 228)
                {
                    model.PublicApproval = Math.Min(100, model.PublicApproval + 5.0);
                    AddFloatingText("🦌 Yaban Hayatı Sevildi! +%5 Halk 🌿", x, y - 10, Color.LightGreen);
                    SoundEngine.PlayEcoAction();
                    return true;
                }
            }

            // Mine Shaft & Elevator (Kömür Ocağı)
            if (x >= 75 && x <= 230 && y >= 95 && y <= 270)
            {
                string res = model.ActionMineCoalShift();
                if (rnd.Next(100) < 18)
                {
                    model.Budget += 6000;
                    AddFloatingText("💎 Zengin Damar! +₺6,000 ⛏️", x, y - 25, Color.FromArgb(241, 196, 15));
                }
                else
                {
                    AddFloatingText("+₺4,500 ⛏️ Kömür Çıkarıldı!", x, y - 10, Color.FromArgb(241, 196, 15));
                }
                return true;
            }

            // Factory & Chimneys (Bacalar & Arıtma)
            if (x >= 350 && x <= 520 && y >= 75 && y <= 230)
            {
                string res = model.ActionChemicalScrub();
                if (rnd.Next(100) < 18)
                {
                    model.NetCarbonEmissions = Math.Max(0, model.NetCarbonEmissions - 35);
                    AddFloatingText("🧪 Katalizör Sürprizi! -80 t CO₂", x, y - 25, Color.Cyan);
                }
                else
                {
                    AddFloatingText("-45 t CO₂ 🏭 Baca Yıkandı!", x, y - 10, Color.FromArgb(46, 204, 113));
                }
                return true;
            }

            // Green Forest Buffer (Ağaç Dikimi)
            if (((x >= 10 && x <= 130) || (x >= 530 && x <= 740)) && (y >= 150 && y <= 230))
            {
                string res = model.ActionPlantTrees();
                if (rnd.Next(100) < 18)
                {
                    model.PublicApproval = Math.Min(100, model.PublicApproval + 6.0);
                    AddFloatingText("🌸 Doğal Çiçeklenme! +%6 Halk", x, y - 25, Color.FromArgb(255, 105, 180));
                }
                else
                {
                    AddFloatingText("🌱 Fidan Dikildi!", x, y - 10, Color.FromArgb(100, 240, 140));
                }
                return true;
            }

            // Wind & Solar Ridge (Yenilenebilir Güç)
            if (x >= 750 && x <= 1180 && y >= 70 && y <= 230)
            {
                string res = model.ActionInspectRenewables();
                if (rnd.Next(100) < 18)
                {
                    model.RenewableEnergyOutput += 60.0;
                    AddFloatingText("⚡ Temiz Enerji Patlaması! +100 MWh", x, y - 25, Color.FromArgb(90, 210, 255));
                }
                else
                {
                    AddFloatingText("⚡ +40 MWh Temiz Enerji!", x, y - 10, Color.FromArgb(90, 200, 255));
                }
                return true;
            }

            return false;
        }

        public void Render(Graphics g, int width, int height, GameModel model)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;

            // 1. SKY (multi-layer gradient, stars, atmospheric glow)
            DrawSkyAndAtmosphere(g, width, height, model);

            // 2. SUN & SMOG FILTER
            DrawSunAndSmog(g, width, height, model);

            // 2.5 CLEAN VICTORY RAINBOW (When Net Carbon <= 0 & clean energy active!)
            if (model.NetCarbonEmissions <= 0 && model.RenewableEnergyOutput > 0)
            {
                DrawCleanVictoryRainbow(g, width, height);
            }

            // 3. BIRDS (clean sky only)
            DrawBirds(g, width, height, model);

            // 4. BACKGROUND MOUNTAINS (3 layers with snow caps)
            DrawMountainsAndHills(g, width, height, model);

            // 5. CLEAN ENERGY INSTALLATIONS
            DrawRenewables(g, width, height, model);

            // 6. ECO FOREST GREEN BELT (multiple tree types)
            DrawForestBuffer(g, width, height, model);

            // 6.5 WILDLIFE (Deer grazing if forest healthy)
            DrawWildlife(g, 65f, 220f, model);

            // 7. RIVER / STREAM
            DrawRiver(g, width, height, model);

            // 8. GROUND DETAILS (rocks, grass, dirt road)
            DrawGroundDetails(g, width, height, model);

            // 9. SURFACE MINE & INDUSTRIAL COMPLEX (brick, admin bldg, conveyor)
            DrawSurfaceMineComplex(g, width, height, model);

            // 10. CHIMNEY HEAT SHIMMER
            DrawHeatShimmer(g, model);

            // 11. UNDERGROUND CROSS-SECTION (strata, arched tunnel, ore veins, drips)
            DrawUndergroundSection(g, width, height, model);

            // 12. SMOKE PARTICLES (improved with growth & transparency)
            DrawSmokeParticles(g);

            // 12.5 SEASONAL WEATHER PARTICLES (Winter Snow / Autumn Leaves)
            int currentM = ((model.CurrentMonth - 1) % 12) + 1;
            if (currentM == 12 || currentM <= 2)
            {
                DrawFallingSnow(g, width, height);
            }
            else if (currentM >= 9 && currentM <= 11)
            {
                DrawFallingAutumnLeaves(g, width, height);
            }

            // 13. ACID RAIN OR TOXIC SMOG OVERLAYS
            if (model.ClimateThreatPercent > 65.0)
            {
                DrawSevereWeatherAlert(g, width, height, model);
            }

            // 14. INTERACTIVE CLICKABLE ZONE BADGES
            DrawInteractiveZoneBadges(g, model);

            // 15. FLOATING ACTION TEXTS
            DrawFloatingTexts(g);
        }

        private void DrawInteractiveZoneBadges(Graphics g, GameModel model)
        {
            float pulse = (float)(Math.Sin(pulsePhase * 2.0) * 0.2 + 0.8);
            int alpha = (int)(pulse * 180);

            using (Font badgeFont = new Font("Segoe UI", 7.2f, FontStyle.Bold))
            {
                // 1. Mine Shaft badge
                DrawClickBadge(g, badgeFont, "⛏️ TIKLA: KÖMÜR ÇIKAR", 140, 95, Color.FromArgb(alpha, 241, 196, 15), Color.Black);

                // 2. Smokestacks badge
                DrawClickBadge(g, badgeFont, "🏭 TIKLA: BACA YIKA", 445, 90, Color.FromArgb(alpha, 46, 204, 113), Color.Black);

                // 3. Forest badge
                DrawClickBadge(g, badgeFont, "🌲 TIKLA: FİDAN DİK", 50, 200, Color.FromArgb(alpha, 52, 152, 219), Color.White);

                // 4. Renewables badge (if any upgrade active)
                if (model.RenewableEnergyOutput > 0)
                {
                    DrawClickBadge(g, badgeFont, "⚡ TIKLA: TEMİZ ENERJİ", 940, 115, Color.FromArgb(alpha, 155, 89, 182), Color.White);
                }
            }
        }

        private void DrawClickBadge(Graphics g, Font font, string text, float centerX, float y, Color bgColor, Color textColor)
        {
            SizeF sz = g.MeasureString(text, font);
            float x = centerX - sz.Width / 2;
            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            using (SolidBrush txtBrush = new SolidBrush(textColor))
            using (Pen borderPen = new Pen(Color.FromArgb(200, 255, 255, 255), 1f))
            {
                g.FillRectangle(bgBrush, x - 5, y - 2, sz.Width + 10, sz.Height + 4);
                g.DrawRectangle(borderPen, x - 5, y - 2, sz.Width + 10, sz.Height + 4);
                g.DrawString(text, font, txtBrush, x, y);
            }
        }

        // =====================================================
        // 1. SKY - Multiple gradient layers, stars, atmo glow
        // =====================================================
        private void DrawSkyAndAtmosphere(Graphics g, int width, int height, GameModel model)
        {
            float threat = (float)Math.Min(100.0, Math.Max(0.0, model.ClimateThreatPercent));
            float groundY = 220f;

            // --- Base sky gradient ---
            Color topSky, midSky, bottomSky;
            if (threat < 35.0f)
            {
                topSky = Color.FromArgb(25, 80, 160);
                midSky = Color.FromArgb(65, 145, 210);
                bottomSky = Color.FromArgb(174, 214, 241);
            }
            else if (threat < 65.0f)
            {
                float t = (threat - 35.0f) / 30.0f;
                topSky = InterpolateColor(Color.FromArgb(25, 80, 160), Color.FromArgb(100, 95, 65), t);
                midSky = InterpolateColor(Color.FromArgb(65, 145, 210), Color.FromArgb(140, 130, 95), t);
                bottomSky = InterpolateColor(Color.FromArgb(174, 214, 241), Color.FromArgb(195, 185, 140), t);
            }
            else if (threat < 85.0f)
            {
                float t = (threat - 65.0f) / 20.0f;
                topSky = InterpolateColor(Color.FromArgb(100, 95, 65), Color.FromArgb(90, 45, 30), t);
                midSky = InterpolateColor(Color.FromArgb(140, 130, 95), Color.FromArgb(120, 70, 50), t);
                bottomSky = InterpolateColor(Color.FromArgb(195, 185, 140), Color.FromArgb(165, 110, 75), t);
            }
            else
            {
                float t = (threat - 85.0f) / 15.0f;
                topSky = InterpolateColor(Color.FromArgb(90, 45, 30), Color.FromArgb(35, 15, 25), t);
                midSky = InterpolateColor(Color.FromArgb(120, 70, 50), Color.FromArgb(55, 28, 38), t);
                bottomSky = InterpolateColor(Color.FromArgb(165, 110, 75), Color.FromArgb(75, 40, 50), t);
            }

            // Draw multi-band sky: top -> mid -> bottom
            int skyMid = (int)(groundY * 0.45f);
            using (LinearGradientBrush topBand = new LinearGradientBrush(
                new Point(0, 0), new Point(0, skyMid), topSky, midSky))
            {
                g.FillRectangle(topBand, 0, 0, width, skyMid);
            }
            using (LinearGradientBrush botBand = new LinearGradientBrush(
                new Point(0, skyMid - 1), new Point(0, (int)groundY), midSky, bottomSky))
            {
                g.FillRectangle(botBand, 0, skyMid - 1, width, (int)groundY - skyMid + 2);
            }

            // --- Atmospheric glow near horizon ---
            int glowH = 30;
            int glowY = (int)groundY - glowH;
            Color glowCol = InterpolateColor(Color.FromArgb(255, 230, 180), Color.FromArgb(200, 140, 80), threat / 100f);
            using (LinearGradientBrush glowBrush = new LinearGradientBrush(
                new Point(0, glowY), new Point(0, (int)groundY),
                Color.FromArgb(0, glowCol), Color.FromArgb(55, glowCol)))
            {
                g.FillRectangle(glowBrush, 0, glowY, width, glowH);
            }

            // --- Stars visible when sky is clean ---
            if (threat < 40f)
            {
                int starAlpha = (int)(180 * (1f - threat / 40f));
                using (SolidBrush starBrush = new SolidBrush(Color.FromArgb(starAlpha, 255, 255, 255)))
                {
                    // Deterministic star positions using a seeded pattern
                    int starSeed = 42;
                    Random starRng = new Random(starSeed);
                    int numStars = 35;
                    for (int s = 0; s < numStars; s++)
                    {
                        float sx = (float)(starRng.NextDouble() * width);
                        float sy = (float)(starRng.NextDouble() * groundY * 0.6);
                        float sr = (float)(starRng.NextDouble() * 1.5 + 0.5);
                        // Twinkle effect
                        float twinkle = (float)Math.Sin(pulsePhase * 2f + s * 0.7f);
                        int sa = (int)Math.Max(30, starAlpha * (0.6f + 0.4f * twinkle));
                        using (SolidBrush twinkleBrush = new SolidBrush(Color.FromArgb(sa, 255, 255, 240)))
                        {
                            g.FillEllipse(twinkleBrush, sx - sr, sy - sr, sr * 2, sr * 2);
                        }
                    }
                }
            }

            // --- Drifting Fluffy Clouds ---
            float cloudAlpha = Math.Max(20f, 220f - threat * 2.0f);
            using (SolidBrush cloudBrush = new SolidBrush(Color.FromArgb((int)cloudAlpha, 255, 255, 255)))
            {
                DrawCloud(g, cloudBrush, (100f + cloudOffset) % (width + 300) - 150f, 40f, 75f);
                DrawCloud(g, cloudBrush, (450f + cloudOffset * 0.8f) % (width + 300) - 150f, 65f, 90f);
                DrawCloud(g, cloudBrush, (850f + cloudOffset * 1.1f) % (width + 300) - 150f, 30f, 65f);
            }
            // Secondary translucent cloud layer
            float cl2Alpha = Math.Max(10f, 110f - threat * 1.2f);
            using (SolidBrush cl2 = new SolidBrush(Color.FromArgb((int)cl2Alpha, 240, 245, 255)))
            {
                DrawCloud(g, cl2, (250f + cloudOffset * 0.5f) % (width + 300) - 100f, 80f, 55f);
                DrawCloud(g, cl2, (700f + cloudOffset * 0.65f) % (width + 300) - 100f, 50f, 48f);
            }
        }

        private void DrawCloud(Graphics g, Brush brush, float cx, float cy, float w)
        {
            float h = w * 0.45f;
            g.FillEllipse(brush, cx, cy, w, h);
            g.FillEllipse(brush, cx + w * 0.15f, cy - h * 0.45f, w * 0.55f, h * 0.85f);
            g.FillEllipse(brush, cx + w * 0.4f, cy - h * 0.35f, w * 0.5f, h * 0.8f);
            g.FillEllipse(brush, cx + w * 0.6f, cy - h * 0.15f, w * 0.35f, h * 0.65f);
        }

        // =====================================================
        // 2. SUN & SMOG
        // =====================================================
        private void DrawSunAndSmog(Graphics g, int width, int height, GameModel model)
        {
            float threat = (float)model.ClimateThreatPercent;
            float sunX = width - 130f;
            float sunY = 55f;
            float sunR = 36f;

            Color sunCol;
            if (threat < 40f)
            {
                sunCol = Color.FromArgb(255, 235, 120);
                // Outer soft glow
                using (SolidBrush outerGlow = new SolidBrush(Color.FromArgb(18, 255, 240, 100)))
                {
                    g.FillEllipse(outerGlow, sunX - sunR * 2.8f, sunY - sunR * 2.8f, sunR * 5.6f, sunR * 5.6f);
                }
                // Inner warm halo
                using (SolidBrush halo = new SolidBrush(Color.FromArgb(40, 255, 220, 80)))
                {
                    g.FillEllipse(halo, sunX - sunR * 1.8f, sunY - sunR * 1.8f, sunR * 3.6f, sunR * 3.6f);
                }
                // Sun rays
                using (Pen rayPen = new Pen(Color.FromArgb(30, 255, 230, 100), 1.5f))
                {
                    for (int r = 0; r < 12; r++)
                    {
                        float rad = (float)(r * 30.0 * Math.PI / 180.0);
                        float innerR = sunR * 1.3f;
                        float outerR = sunR * 2.2f + (float)Math.Sin(pulsePhase + r) * 4f;
                        g.DrawLine(rayPen,
                            sunX + (float)Math.Cos(rad) * innerR,
                            sunY + (float)Math.Sin(rad) * innerR,
                            sunX + (float)Math.Cos(rad) * outerR,
                            sunY + (float)Math.Sin(rad) * outerR);
                    }
                }
            }
            else if (threat < 75f)
            {
                sunCol = Color.FromArgb(240, 160, 90);
                using (SolidBrush halo = new SolidBrush(Color.FromArgb(25, 240, 140, 60)))
                {
                    g.FillEllipse(halo, sunX - sunR * 1.5f, sunY - sunR * 1.5f, sunR * 3f, sunR * 3f);
                }
            }
            else
            {
                sunCol = Color.FromArgb(190, 70, 70);
                using (SolidBrush halo = new SolidBrush(Color.FromArgb(20, 190, 50, 50)))
                {
                    g.FillEllipse(halo, sunX - sunR * 1.4f, sunY - sunR * 1.4f, sunR * 2.8f, sunR * 2.8f);
                }
            }

            using (SolidBrush sunBrush = new SolidBrush(sunCol))
            {
                g.FillEllipse(sunBrush, sunX - sunR, sunY - sunR, sunR * 2, sunR * 2);
            }
        }

        // =====================================================
        // 3. BIRDS (V-shapes, visible when clean sky)
        // =====================================================
        private void DrawBirds(Graphics g, int width, int height, GameModel model)
        {
            float threat = (float)model.ClimateThreatPercent;
            if (threat >= 45f) return;
            if (birdX == null) return;

            int birdAlpha = (int)(200 * (1f - threat / 45f));
            using (Pen birdPen = new Pen(Color.FromArgb(birdAlpha, 30, 30, 40), 1.5f))
            {
                for (int i = 0; i < birdX.Length; i++)
                {
                    float bx = birdX[i];
                    float by = birdY[i] + (float)Math.Sin(birdPhase * 3f + i * 2f) * 3f;
                    float wingFlap = (float)Math.Sin(pulsePhase * 5f + i * 1.5f) * 3f;
                    // Left wing
                    g.DrawLine(birdPen, bx, by, bx - 5, by - 3 - wingFlap);
                    // Right wing
                    g.DrawLine(birdPen, bx, by, bx + 5, by - 3 - wingFlap);
                }
            }
        }

        // =====================================================
        // 4. MOUNTAINS - 3 layers with snow caps
        // =====================================================
        private void DrawMountainsAndHills(Graphics g, int width, int height, GameModel model)
        {
            float threat = (float)model.ClimateThreatPercent;
            float groundY = 220f;

            // --- Layer 1: Far mountains (blue-gray, tallest) ---
            Color farColor = InterpolateColor(Color.FromArgb(80, 100, 135), Color.FromArgb(75, 65, 60), threat / 100f);
            using (SolidBrush farBrush = new SolidBrush(farColor))
            {
                PointF[] farRidge = new PointF[]
                {
                    new PointF(0, groundY),
                    new PointF(0, 130),
                    new PointF(60, 110),
                    new PointF(140, 72),
                    new PointF(220, 100),
                    new PointF(310, 115),
                    new PointF(420, 80),
                    new PointF(520, 110),
                    new PointF(640, 130),
                    new PointF(750, 90),
                    new PointF(860, 65),
                    new PointF(960, 95),
                    new PointF(1050, 78),
                    new PointF(1120, 110),
                    new PointF(width, 120),
                    new PointF(width, groundY)
                };
                g.FillPolygon(farBrush, farRidge);
            }

            // Snow caps on far peaks
            Color snowColor = InterpolateColor(Color.FromArgb(240, 245, 250), Color.FromArgb(180, 170, 160), threat / 100f);
            using (SolidBrush snowBrush = new SolidBrush(snowColor))
            {
                // Peak at x=140, y=72
                g.FillPolygon(snowBrush, new PointF[] {
                    new PointF(140, 72), new PointF(125, 85), new PointF(155, 85)
                });
                // Peak at x=860, y=65
                g.FillPolygon(snowBrush, new PointF[] {
                    new PointF(860, 65), new PointF(843, 80), new PointF(877, 80)
                });
                // Peak at x=1050, y=78
                g.FillPolygon(snowBrush, new PointF[] {
                    new PointF(1050, 78), new PointF(1038, 90), new PointF(1062, 90)
                });
            }

            // --- Layer 2: Mid mountains (green-gray) ---
            Color midColor = InterpolateColor(Color.FromArgb(60, 90, 75), Color.FromArgb(90, 78, 55), threat / 100f);
            using (SolidBrush midBrush = new SolidBrush(midColor))
            {
                PointF[] midRidge = new PointF[]
                {
                    new PointF(0, groundY),
                    new PointF(0, 160),
                    new PointF(100, 140),
                    new PointF(200, 125),
                    new PointF(320, 148),
                    new PointF(480, 130),
                    new PointF(600, 155),
                    new PointF(720, 135),
                    new PointF(840, 120),
                    new PointF(950, 140),
                    new PointF(1080, 128),
                    new PointF(width, 145),
                    new PointF(width, groundY)
                };
                g.FillPolygon(midBrush, midRidge);
            }

            // --- Layer 3: Near hills (dark green, rolling) ---
            Color nearColor = InterpolateColor(Color.FromArgb(40, 110, 45), Color.FromArgb(110, 90, 55), threat / 100f);
            using (SolidBrush nearBrush = new SolidBrush(nearColor))
            {
                PointF[] nearHills = new PointF[]
                {
                    new PointF(0, groundY),
                    new PointF(0, 185),
                    new PointF(120, 178),
                    new PointF(240, 170),
                    new PointF(380, 180),
                    new PointF(540, 190),
                    new PointF(680, 175),
                    new PointF(840, 150),
                    new PointF(960, 162),
                    new PointF(1060, 165),
                    new PointF(width, 178),
                    new PointF(width, groundY)
                };
                g.FillPolygon(nearBrush, nearHills);
            }
        }

        private UpgradeItem FindUpgrade(GameModel model, string id)
        {
            if (model == null || model.Upgrades == null) return null;
            for (int i = 0; i < model.Upgrades.Count; i++)
            {
                if (model.Upgrades[i].Id == id) return model.Upgrades[i];
            }
            return null;
        }

        // =====================================================
        // 5. RENEWABLES - Better turbines, solar cells, CCS dome
        // =====================================================
        private void DrawRenewables(Graphics g, int width, int height, GameModel model)
        {
            UpgradeItem windUp = FindUpgrade(model, "wind");
            UpgradeItem solarUp = FindUpgrade(model, "solar");
            UpgradeItem ccsUp = FindUpgrade(model, "ccs");

            // WIND TURBINES
            if (windUp != null && windUp.Level > 0)
            {
                int turbineCount = windUp.Level;
                float[] tx = new float[] { 850f, 960f, 1070f };
                float[] ty = new float[] { 152f, 160f, 168f };
                float[] th = new float[] { 68f, 62f, 56f };

                for (int t = 0; t < turbineCount; t++)
                {
                    DrawSingleWindTurbine(g, tx[t], ty[t], th[t], turbineAngle + t * 45f);
                }
            }

            // SOLAR FARM
            if (solarUp != null && solarUp.Level > 0)
            {
                int panelRows = solarUp.Level;
                for (int r = 0; r < panelRows; r++)
                {
                    DrawSolarPanelRow(g, 620f + r * 55f, 185f - r * 10f, 48f, 22f);
                }
            }

            // CCS DOME
            if (ccsUp != null && ccsUp.Level > 0)
            {
                DrawCcsInstallation(g, 540f, 205f, ccsUp.Level);
            }
        }

        private void DrawSingleWindTurbine(Graphics g, float x, float groundY, float towerH, float angleDeg)
        {
            // Tower - slightly tapered
            using (Pen towerPen = new Pen(Color.FromArgb(220, 225, 230), 3.5f))
            {
                g.DrawLine(towerPen, x, groundY, x, groundY - towerH);
            }
            // Thinner top
            using (Pen topPen = new Pen(Color.FromArgb(230, 235, 240), 2.5f))
            {
                g.DrawLine(topPen, x, groundY - towerH * 0.5f, x, groundY - towerH);
            }

            float hubX = x;
            float hubY = groundY - towerH;

            // Nacelle housing
            using (SolidBrush nacelle = new SolidBrush(Color.FromArgb(210, 215, 220)))
            {
                g.FillRectangle(nacelle, hubX - 6, hubY - 4, 12, 7);
            }

            // Hub center
            using (SolidBrush hubBrush = new SolidBrush(Color.FromArgb(200, 205, 210)))
            {
                g.FillEllipse(hubBrush, hubX - 4, hubY - 4, 8, 8);
            }

            // 3 Tapered Blades
            float bladeLen = towerH * 0.55f;
            for (int b = 0; b < 3; b++)
            {
                float rad = (float)((angleDeg + b * 120.0) * Math.PI / 180.0);
                float bx = hubX + (float)Math.Cos(rad) * bladeLen;
                float by = hubY + (float)Math.Sin(rad) * bladeLen;

                // Blade with taper (wider at hub, thinner at tip)
                float perpX = -(float)Math.Sin(rad);
                float perpY = (float)Math.Cos(rad);
                float baseW = 2.5f;
                float tipW = 0.5f;

                PointF[] bladePoly = new PointF[]
                {
                    new PointF(hubX + perpX * baseW, hubY + perpY * baseW),
                    new PointF(hubX - perpX * baseW, hubY - perpY * baseW),
                    new PointF(bx - perpX * tipW, by - perpY * tipW),
                    new PointF(bx + perpX * tipW, by + perpY * tipW)
                };

                using (SolidBrush bladeBrush = new SolidBrush(Color.FromArgb(230, 240, 245, 250)))
                {
                    g.FillPolygon(bladeBrush, bladePoly);
                }
            }

            // Red aircraft warning light at top
            float lightAlpha = (float)(Math.Sin(blinkTimer * 3f) * 0.5 + 0.5);
            int la = (int)(lightAlpha * 255);
            using (SolidBrush lightBrush = new SolidBrush(Color.FromArgb(la, 255, 30, 30)))
            {
                g.FillEllipse(lightBrush, hubX - 2, hubY - towerH * 0.02f - 7, 4, 4);
            }
        }

        private void DrawSolarPanelRow(Graphics g, float x, float y, float w, float h)
        {
            // Mounting pole / structure
            using (Pen polePen = new Pen(Color.FromArgb(140, 140, 145), 2f))
            {
                g.DrawLine(polePen, x + w * 0.3f, y + h, x + w * 0.3f, y + h + 12);
                g.DrawLine(polePen, x + w * 0.7f, y + h, x + w * 0.7f, y + h + 12);
                // Cross brace
                g.DrawLine(polePen, x + w * 0.3f, y + h + 6, x + w * 0.7f, y + h + 6);
            }

            // Slanted panel body
            PointF[] panelPoly = new PointF[]
            {
                new PointF(x, y),
                new PointF(x + w, y - 5),
                new PointF(x + w + 12, y + h - 5),
                new PointF(x + 12, y + h)
            };

            using (SolidBrush solarBrush = new SolidBrush(Color.FromArgb(22, 48, 88)))
            {
                g.FillPolygon(solarBrush, panelPoly);
            }

            // Individual cell grid pattern
            using (Pen cellPen = new Pen(Color.FromArgb(55, 100, 170), 0.7f))
            {
                g.DrawPolygon(cellPen, panelPoly);
                // Horizontal cell lines
                for (int row = 1; row < 4; row++)
                {
                    float frac = row / 4f;
                    float lx1 = x + (x + 12 - x) * frac;
                    float ly1 = y + (y + h - y) * frac;
                    float lx2 = (x + w) + ((x + w + 12) - (x + w)) * frac;
                    float ly2 = (y - 5) + ((y + h - 5) - (y - 5)) * frac;
                    g.DrawLine(cellPen, lx1, ly1, lx2, ly2);
                }
                // Vertical cell lines
                for (int col = 1; col < 6; col++)
                {
                    float frac = col / 6f;
                    float topX1 = x + (x + w) * 0f + frac * w;
                    float topY1 = y + frac * (-5f);
                    float botX1 = (x + 12) + frac * w;
                    float botY1 = (y + h) + frac * (-5f);
                    g.DrawLine(cellPen, topX1, topY1, botX1, botY1);
                }
            }

            // Flare reflection glint
            float flareAlpha = (float)(Math.Sin(pulsePhase * 2.0f) * 60 + 60);
            using (SolidBrush flareBrush = new SolidBrush(Color.FromArgb((int)flareAlpha, 255, 255, 255)))
            {
                g.FillEllipse(flareBrush, x + 15, y + 4, 10, 6);
            }
        }

        private void DrawCcsInstallation(Graphics g, float x, float y, int level)
        {
            // Sci-fi pressurized dome - larger, more detailed
            float domeW = 56f;
            float domeH = 48f;

            // Dome body with gradient feel (draw multiple arcs)
            using (SolidBrush domeBrush = new SolidBrush(Color.FromArgb(50, 80, 105)))
            {
                g.FillPie(domeBrush, x - domeW / 2, y - domeH + 8, domeW, domeH, 180, 180);
            }
            // Dome highlight
            using (SolidBrush domeHighlight = new SolidBrush(Color.FromArgb(40, 120, 180, 220)))
            {
                g.FillPie(domeHighlight, x - domeW / 2 + 5, y - domeH + 12, domeW - 10, domeH - 8, 200, 140);
            }

            // Glowing rings around dome
            for (int ring = 0; ring < 3; ring++)
            {
                float ringOffset = ring * 8f;
                float ringPulse = (float)Math.Sin(pulsePhase * 2.5f + ring * 1.2f);
                int ringAlpha = (int)(80 + ringPulse * 40);
                using (Pen ringPen = new Pen(Color.FromArgb(ringAlpha, 80, 200, 255), 1.5f))
                {
                    g.DrawArc(ringPen, x - 18 + ringOffset / 2, y - 28 + ringOffset, 36 - ringOffset, 20 - ringOffset / 2, 180, 180);
                }
            }

            // Central pulsing energy indicator
            float pulseR = 6f + (float)Math.Sin(pulsePhase * 3.0f) * 2.5f;
            using (SolidBrush pulseGlow = new SolidBrush(Color.FromArgb(120, 60, 220, 255)))
            {
                g.FillEllipse(pulseGlow, x - pulseR, y - 22 - pulseR, pulseR * 2, pulseR * 2);
            }
            using (Pen pulsePen = new Pen(Color.FromArgb(180, 100, 220, 255), 1.5f))
            {
                g.DrawEllipse(pulsePen, x - pulseR, y - 22 - pulseR, pulseR * 2, pulseR * 2);
            }

            // Pipe network going underground (branching)
            using (Pen pipePen = new Pen(Color.FromArgb(60, 140, 170), 3f))
            {
                // Main vertical pipe
                g.DrawLine(pipePen, x, y, x, y + 55);
            }
            using (Pen pipePen2 = new Pen(Color.FromArgb(50, 120, 150), 2f))
            {
                // Branch pipes
                g.DrawLine(pipePen2, x, y + 25, x - 15, y + 45);
                g.DrawLine(pipePen2, x, y + 25, x + 15, y + 45);
                // Deeper branches
                g.DrawLine(pipePen2, x - 15, y + 45, x - 20, y + 55);
                g.DrawLine(pipePen2, x + 15, y + 45, x + 20, y + 55);
            }

            // Pipe junction dots
            using (SolidBrush junctionBrush = new SolidBrush(Color.FromArgb(100, 180, 220)))
            {
                g.FillEllipse(junctionBrush, x - 3, y + 22, 6, 6);
            }

            // Level indicator lights
            for (int lv = 0; lv < level; lv++)
            {
                using (SolidBrush lvBrush = new SolidBrush(Color.FromArgb(180, 80, 255, 120)))
                {
                    g.FillEllipse(lvBrush, x - 14 + lv * 14, y - 8, 5, 5);
                }
            }
        }

        // =====================================================
        // 6. FOREST - Multiple tree types (pine, deciduous, bush)
        // =====================================================
        private void DrawForestBuffer(Graphics g, int width, int height, GameModel model)
        {
            UpgradeItem forestUp = FindUpgrade(model, "forest");
            if (forestUp == null || forestUp.Level == 0) return;

            int treeCount = 6 + forestUp.Level * 6;
            float startX = 20f;
            float step = 28f;

            for (int i = 0; i < treeCount; i++)
            {
                float tx = startX + i * step;
                if (tx > 380f && tx < 520f) continue;

                float sway = (float)Math.Sin(treeSway + i * 0.8f) * 1.5f;
                float ty = 216f + (float)Math.Sin(i * 1.5) * 4f;

                int treeType = i % 5;
                if (treeType < 2)
                {
                    DrawPineTree(g, tx + sway, ty, 28f + (i % 3) * 6f);
                }
                else if (treeType < 4)
                {
                    DrawDeciduousTree(g, tx + sway, ty, 22f + (i % 3) * 4f);
                }
                else
                {
                    DrawBush(g, tx + sway, ty, 10f + (i % 3) * 3f);
                }
            }
        }

        private void DrawPineTree(Graphics g, float x, float groundY, float h)
        {
            // Trunk
            using (SolidBrush trunkBrush = new SolidBrush(Color.FromArgb(85, 55, 35)))
            {
                g.FillRectangle(trunkBrush, x - 2, groundY - 8, 4, 8);
            }

            // 3-tier foliage (dark green pines)
            using (SolidBrush pineBrush = new SolidBrush(Color.FromArgb(28, 95, 38)))
            {
                g.FillPolygon(pineBrush, new PointF[] {
                    new PointF(x, groundY - h * 0.5f),
                    new PointF(x - h * 0.45f, groundY - 6),
                    new PointF(x + h * 0.45f, groundY - 6)
                });
            }
            using (SolidBrush pineBrush2 = new SolidBrush(Color.FromArgb(32, 105, 42)))
            {
                g.FillPolygon(pineBrush2, new PointF[] {
                    new PointF(x, groundY - h * 0.75f),
                    new PointF(x - h * 0.35f, groundY - h * 0.35f),
                    new PointF(x + h * 0.35f, groundY - h * 0.35f)
                });
            }
            using (SolidBrush pineBrush3 = new SolidBrush(Color.FromArgb(36, 115, 48)))
            {
                g.FillPolygon(pineBrush3, new PointF[] {
                    new PointF(x, groundY - h),
                    new PointF(x - h * 0.25f, groundY - h * 0.6f),
                    new PointF(x + h * 0.25f, groundY - h * 0.6f)
                });
            }
        }

        private void DrawDeciduousTree(Graphics g, float x, float groundY, float h)
        {
            // Trunk
            using (SolidBrush trunkBrush = new SolidBrush(Color.FromArgb(100, 65, 40)))
            {
                g.FillRectangle(trunkBrush, x - 2, groundY - 10, 4, 10);
            }

            // Round canopy (multiple overlapping circles)
            float canopyY = groundY - h * 0.6f;
            float canopyR = h * 0.4f;

            using (SolidBrush canopy1 = new SolidBrush(Color.FromArgb(45, 130, 55)))
            {
                g.FillEllipse(canopy1, x - canopyR, canopyY - canopyR, canopyR * 2, canopyR * 2);
            }
            using (SolidBrush canopy2 = new SolidBrush(Color.FromArgb(55, 145, 60)))
            {
                g.FillEllipse(canopy2, x - canopyR * 0.7f, canopyY - canopyR * 1.2f, canopyR * 1.4f, canopyR * 1.4f);
            }
            using (SolidBrush canopy3 = new SolidBrush(Color.FromArgb(50, 138, 58)))
            {
                g.FillEllipse(canopy3, x + canopyR * 0.2f, canopyY - canopyR * 0.5f, canopyR * 1.1f, canopyR * 1.1f);
            }
        }

        private void DrawBush(Graphics g, float x, float groundY, float h)
        {
            float bushR = h * 0.5f;
            using (SolidBrush bushBrush = new SolidBrush(Color.FromArgb(50, 120, 45)))
            {
                g.FillEllipse(bushBrush, x - bushR, groundY - bushR * 1.2f, bushR * 2, bushR * 1.4f);
            }
            using (SolidBrush bushHighlight = new SolidBrush(Color.FromArgb(58, 135, 52)))
            {
                g.FillEllipse(bushHighlight, x - bushR * 0.5f, groundY - bushR * 1.4f, bushR * 1.2f, bushR);
            }
        }

        // =====================================================
        // 6.5 WILDLIFE (DEER & FAWN)
        // =====================================================
        private void DrawWildlife(Graphics g, float x, float groundY, GameModel model)
        {
            if (model.ForestAbsorptionRate < 20.0 || model.ClimateThreatPercent >= 65.0) return;

            float y = groundY - 14f;
            float grazeOffset = (float)(Math.Sin(pulsePhase * 2.0) * 1.5);

            using (SolidBrush coat = new SolidBrush(Color.FromArgb(175, 115, 60)))
            using (SolidBrush belly = new SolidBrush(Color.FromArgb(235, 210, 180)))
            using (Pen legPen = new Pen(Color.FromArgb(140, 85, 40), 1.6f))
            using (Pen antlerPen = new Pen(Color.FromArgb(100, 70, 45), 1.2f))
            {
                // Legs
                g.DrawLine(legPen, x + 3, y + 8, x + 2, y + 15);
                g.DrawLine(legPen, x + 7, y + 8, x + 7, y + 15);
                g.DrawLine(legPen, x + 15, y + 8, x + 15, y + 15);
                g.DrawLine(legPen, x + 19, y + 8, x + 20, y + 15);

                // Body torso
                g.FillEllipse(coat, x + 2, y + 2, 19, 9);
                g.FillEllipse(belly, x + 5, y + 6, 12, 4);

                // Neck & Head with grazing movement
                float headY = y - 4 + grazeOffset;
                g.FillEllipse(coat, x - 3, headY, 8, 6);

                // Tiny Antlers
                g.DrawLine(antlerPen, x - 1, headY + 1, x - 3, headY - 4);
                g.DrawLine(antlerPen, x - 3, headY - 4, x - 5, headY - 6);
                g.DrawLine(antlerPen, x - 3, headY - 4, x - 1, headY - 7);

                // Tail (white tuft)
                using (SolidBrush tailBrush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(tailBrush, x + 20, y + 3, 3, 3);
                }
            }
        }

        // =====================================================
        // 7. RIVER / STREAM with animated shimmer
        // =====================================================
        private void DrawRiver(Graphics g, int width, int height, GameModel model)
        {
            float groundY = 220f;
            float threat = (float)model.ClimateThreatPercent;

            Color riverColor = InterpolateColor(
                Color.FromArgb(50, 130, 190),
                Color.FromArgb(85, 110, 100),
                threat / 100f);

            // Winding river path across the valley
            PointF[] riverTop = new PointF[]
            {
                new PointF(0, groundY - 6),
                new PointF(60, groundY - 4),
                new PointF(130, groundY - 8),
                new PointF(200, groundY - 3),
                new PointF(280, groundY - 7),
                new PointF(360, groundY - 2),
                new PointF(500, groundY - 5),
                new PointF(580, groundY - 3),
                new PointF(650, groundY - 7),
                new PointF(750, groundY - 4),
                new PointF(830, groundY - 6),
                new PointF(width, groundY - 5)
            };

            PointF[] riverBot = new PointF[]
            {
                new PointF(width, groundY + 2),
                new PointF(830, groundY + 1),
                new PointF(750, groundY + 3),
                new PointF(650, groundY),
                new PointF(580, groundY + 3),
                new PointF(500, groundY + 1),
                new PointF(360, groundY + 4),
                new PointF(280, groundY),
                new PointF(200, groundY + 3),
                new PointF(130, groundY - 1),
                new PointF(60, groundY + 2),
                new PointF(0, groundY)
            };

            // Combine into closed polygon
            PointF[] riverPoly = new PointF[riverTop.Length + riverBot.Length];
            for (int i = 0; i < riverTop.Length; i++)
            {
                riverPoly[i] = riverTop[i];
            }
            for (int i = 0; i < riverBot.Length; i++)
            {
                riverPoly[riverTop.Length + i] = riverBot[i];
            }

            using (SolidBrush riverBrush = new SolidBrush(Color.FromArgb(160, riverColor)))
            {
                g.FillPolygon(riverBrush, riverPoly);
            }

            // Shimmer highlights on the river surface
            int shimmerCount = 12;
            for (int s = 0; s < shimmerCount; s++)
            {
                float sx = (s * 95f + riverShimmerPhase * 30f) % (float)width;
                float sy = groundY - 4f + (float)Math.Sin(riverShimmerPhase * 2f + s * 0.9f) * 2f;
                float shimmerAlpha = (float)(Math.Sin(riverShimmerPhase * 3f + s * 1.3f) * 40 + 50);
                int sa = (int)Math.Max(10, Math.Min(90, shimmerAlpha));
                using (SolidBrush shimBrush = new SolidBrush(Color.FromArgb(sa, 200, 230, 255)))
                {
                    g.FillEllipse(shimBrush, sx, sy, 12, 3);
                }
            }
        }

        // =====================================================
        // 8. GROUND DETAILS - rocks, grass tufts, dirt road
        // =====================================================
        private void DrawGroundDetails(Graphics g, int width, int height, GameModel model)
        {
            float groundY = 220f;
            float threat = (float)model.ClimateThreatPercent;

            // Dirt road / path from left to factory
            using (Pen roadPen = new Pen(Color.FromArgb(110, 90, 65), 5f))
            {
                g.DrawLine(roadPen, 0, groundY - 1, 370, groundY - 1);
            }
            // Road line markings
            using (Pen markPen = new Pen(Color.FromArgb(40, 150, 140, 100), 1f))
            {
                markPen.DashStyle = DashStyle.Dash;
                g.DrawLine(markPen, 0, groundY - 1, 370, groundY - 1);
            }

            // Small rocks
            Color rockColor = Color.FromArgb(120, 110, 100);
            using (SolidBrush rockBrush = new SolidBrush(rockColor))
            {
                // Scattered deterministic rocks
                Random rockRng = new Random(77);
                for (int r = 0; r < 18; r++)
                {
                    float rx = (float)(rockRng.NextDouble() * width);
                    float ry = groundY - 2f + (float)(rockRng.NextDouble() * 4f);
                    float rw = (float)(rockRng.NextDouble() * 5f + 2f);
                    float rh = (float)(rockRng.NextDouble() * 3f + 1.5f);
                    // Avoid river area
                    if (ry > groundY - 8 && ry < groundY + 4) continue;
                    g.FillEllipse(rockBrush, rx, ry, rw, rh);
                }
            }

            // Grass tufts at ground level
            Color grassColor = InterpolateColor(Color.FromArgb(60, 140, 50), Color.FromArgb(100, 95, 55), threat / 100f);
            using (Pen grassPen = new Pen(grassColor, 1.2f))
            {
                Random grassRng = new Random(55);
                for (int gr = 0; gr < 30; gr++)
                {
                    float gx = (float)(grassRng.NextDouble() * width);
                    float gy = groundY - 1f;
                    // Skip around buildings, river
                    if (gx > 360 && gx < 500) continue;
                    float gh = (float)(grassRng.NextDouble() * 5f + 3f);
                    g.DrawLine(grassPen, gx, gy, gx - 1.5f, gy - gh);
                    g.DrawLine(grassPen, gx + 2, gy, gx + 3.5f, gy - gh * 0.8f);
                    g.DrawLine(grassPen, gx + 4, gy, gx + 2.5f, gy - gh * 0.9f);
                }
            }

            // Seasonal ground variations
            int currentM = ((model.CurrentMonth - 1) % 12) + 1;
            if (currentM == 12 || currentM <= 2)
            {
                using (Pen frostPen = new Pen(Color.FromArgb(170, 240, 248, 255), 1.5f))
                {
                    g.DrawLine(frostPen, 0, groundY, width, groundY);
                }
            }
            else if (currentM >= 3 && currentM <= 5)
            {
                Color[] flowerColors = new Color[] {
                    Color.FromArgb(241, 196, 15),
                    Color.FromArgb(255, 105, 180),
                    Color.FromArgb(255, 255, 255),
                    Color.FromArgb(155, 89, 182)
                };
                Random flwRng = new Random(88);
                for (int fl = 0; fl < 24; fl++)
                {
                    float fx = (float)(flwRng.NextDouble() * width);
                    if (fx > 350 && fx < 510) continue;
                    float fy = groundY - (float)(flwRng.NextDouble() * 8f + 2f);
                    Color fc = flowerColors[fl % flowerColors.Length];
                    using (SolidBrush flwBrush = new SolidBrush(fc))
                    {
                        g.FillEllipse(flwBrush, fx, fy, 3f, 3f);
                    }
                }
            }
        }

        // =====================================================
        // 9. SURFACE MINE & INDUSTRIAL COMPLEX
        //    Brick texture, admin building, conveyor belt
        // =====================================================
        private void DrawSurfaceMineComplex(Graphics g, int width, int height, GameModel model)
        {
            float surfaceY = 220f;

            // === Mine Headframe (Elevator Tower) ===
            float towerX = 140f;
            float towerBaseW = 40f;
            float towerTopW = 20f;
            float towerH = 105f;
            float towerTopY = surfaceY - towerH;

            // Steel truss legs
            using (Pen trussPen = new Pen(Color.FromArgb(70, 75, 80), 2.5f))
            {
                g.DrawLine(trussPen, towerX - towerBaseW / 2, surfaceY, towerX - towerTopW / 2, towerTopY);
                g.DrawLine(trussPen, towerX + towerBaseW / 2, surfaceY, towerX + towerTopW / 2, towerTopY);
                // Cross braces
                g.DrawLine(trussPen, towerX - 18, surfaceY - 30, towerX + 15, surfaceY - 60);
                g.DrawLine(trussPen, towerX + 18, surfaceY - 30, towerX - 15, surfaceY - 60);
                g.DrawLine(trussPen, towerX - 14, surfaceY - 65, towerX + 12, towerTopY);
                g.DrawLine(trussPen, towerX + 14, surfaceY - 65, towerX - 12, towerTopY);
            }

            // Headframe rotating sheave wheel
            using (Pen wheelPen = new Pen(Color.FromArgb(200, 205, 210), 2f))
            using (SolidBrush wheelHub = new SolidBrush(Color.FromArgb(50, 50, 55)))
            {
                float wr = 14f;
                g.DrawEllipse(wheelPen, towerX - wr, towerTopY - wr, wr * 2, wr * 2);
                g.FillEllipse(wheelHub, towerX - 3, towerTopY - 3, 6, 6);

                for (int sp = 0; sp < 4; sp++)
                {
                    float sRad = (float)((wheelAngle + sp * 45.0) * Math.PI / 180.0);
                    g.DrawLine(wheelPen, towerX, towerTopY,
                        towerX + (float)Math.Cos(sRad) * wr,
                        towerTopY + (float)Math.Sin(sRad) * wr);
                }
            }

            // Elevator hoist cable
            using (Pen cablePen = new Pen(Color.FromArgb(40, 40, 45), 1.5f))
            {
                g.DrawLine(cablePen, towerX + 12, towerTopY, towerX + 12, surfaceY + 110);
            }

            // === CONVEYOR BELT from mine shaft to factory ===
            float convStartX = 170f;
            float convEndX = 365f;
            float convY = surfaceY - 14f;

            // Conveyor structure (line)
            using (Pen conveyorFrame = new Pen(Color.FromArgb(90, 90, 95), 2.5f))
            {
                g.DrawLine(conveyorFrame, convStartX, convY, convEndX, convY);
            }
            // Support legs
            using (Pen legPen = new Pen(Color.FromArgb(80, 80, 85), 1.5f))
            {
                for (float lx = convStartX + 20; lx < convEndX; lx += 40)
                {
                    g.DrawLine(legPen, lx, convY, lx, surfaceY);
                }
            }

            // Animated coal chunks on conveyor
            using (SolidBrush coalChunk = new SolidBrush(Color.FromArgb(25, 25, 28)))
            {
                for (float cx = convStartX; cx < convEndX; cx += 20f)
                {
                    float animX = cx + conveyorPhase;
                    if (animX > convEndX) animX -= (convEndX - convStartX);
                    if (animX < convStartX) animX += (convEndX - convStartX);
                    g.FillRectangle(coalChunk, animX - 4, convY - 5, 8, 5);
                }
            }

            // === FACTORY BUILDING with brick texture ===
            float plantX = 370f;
            float plantY = surfaceY - 65f;

            // Main building body
            using (SolidBrush plantBrush = new SolidBrush(Color.FromArgb(120, 75, 55)))
            {
                g.FillRectangle(plantBrush, plantX, plantY, 115, 65);
            }

            // Brick pattern
            using (Pen brickPen = new Pen(Color.FromArgb(60, 95, 60, 42), 0.5f))
            {
                int brickW = 12;
                int brickH = 6;
                int row = 0;
                for (float by = plantY + 2; by < plantY + 63; by += brickH)
                {
                    float offsetX = (row % 2 == 0) ? 0 : brickW / 2f;
                    for (float bx = plantX + offsetX; bx < plantX + 115; bx += brickW)
                    {
                        float drawW = Math.Min(brickW, plantX + 115 - bx);
                        if (drawW > 1)
                        {
                            g.DrawRectangle(brickPen, bx, by, drawW, brickH);
                        }
                    }
                    row++;
                }
            }

            // Building border
            using (Pen borderPen = new Pen(Color.FromArgb(45, 48, 52), 2f))
            {
                g.DrawRectangle(borderPen, plantX, plantY, 115, 65);
            }

            // Corrugated metal roof with diagonal lines
            PointF[] roofPoly = new PointF[]
            {
                new PointF(plantX - 4, plantY),
                new PointF(plantX + 57, plantY - 18),
                new PointF(plantX + 119, plantY)
            };
            using (SolidBrush roofBrush = new SolidBrush(Color.FromArgb(85, 90, 95)))
            {
                g.FillPolygon(roofBrush, roofPoly);
            }
            // Corrugation lines
            using (Pen corrPen = new Pen(Color.FromArgb(50, 105, 110, 115), 0.8f))
            {
                for (int cl = 0; cl < 12; cl++)
                {
                    float frac = cl / 12f;
                    float lx1 = plantX - 4 + frac * 61;
                    float ly1 = plantY + frac * (-18);
                    float lx2 = plantX + 57 + frac * 62;
                    float ly2 = plantY - 18 + frac * 18;
                    g.DrawLine(corrPen, lx1, ly1, lx2, ly2);
                }
            }

            // Windows with warm light
            using (SolidBrush winBrush = new SolidBrush(Color.FromArgb(255, 215, 110)))
            {
                for (int w = 0; w < 4; w++)
                {
                    g.FillRectangle(winBrush, plantX + 12 + w * 24, plantY + 22, 14, 18);
                }
            }
            // Window frames
            using (Pen winFrame = new Pen(Color.FromArgb(70, 55, 40), 1f))
            {
                for (int w = 0; w < 4; w++)
                {
                    float wx = plantX + 12 + w * 24;
                    g.DrawRectangle(winFrame, wx, plantY + 22, 14, 18);
                    g.DrawLine(winFrame, wx + 7, plantY + 22, wx + 7, plantY + 40);
                    g.DrawLine(winFrame, wx, plantY + 31, wx + 14, plantY + 31);
                }
            }

            // === ADMINISTRATION BUILDING (next to factory) ===
            float adminX = plantX + 125f;
            float adminY = surfaceY - 38f;

            using (SolidBrush adminBrush = new SolidBrush(Color.FromArgb(160, 155, 145)))
            {
                g.FillRectangle(adminBrush, adminX, adminY, 55, 38);
            }
            // Admin roof
            using (SolidBrush adminRoof = new SolidBrush(Color.FromArgb(100, 70, 50)))
            {
                g.FillRectangle(adminRoof, adminX - 2, adminY - 5, 59, 7);
            }
            // Admin windows
            using (SolidBrush adminWin = new SolidBrush(Color.FromArgb(180, 210, 240)))
            {
                g.FillRectangle(adminWin, adminX + 6, adminY + 8, 10, 12);
                g.FillRectangle(adminWin, adminX + 22, adminY + 8, 10, 12);
                g.FillRectangle(adminWin, adminX + 38, adminY + 8, 10, 12);
            }
            // Admin door
            using (SolidBrush doorBrush = new SolidBrush(Color.FromArgb(80, 60, 45)))
            {
                g.FillRectangle(doorBrush, adminX + 22, adminY + 24, 12, 14);
            }
            // "KARBON-9" sign on admin building
            using (SolidBrush signBg = new SolidBrush(Color.FromArgb(35, 40, 50)))
            using (Font signFont = new Font("Segoe UI", 6.5f, FontStyle.Bold))
            using (SolidBrush signText = new SolidBrush(Color.FromArgb(230, 180, 50)))
            {
                g.FillRectangle(signBg, adminX + 4, adminY - 16, 47, 12);
                g.DrawString("KARBON-9", signFont, signText, adminX + 7, adminY - 16);
            }

            // === SMOKESTACKS (Chimneys) ===
            float ch1X = 438f;
            float ch1Y = plantY - 45f;
            float ch2X = 464f;
            float ch2Y = plantY - 32f;

            using (SolidBrush chBrush = new SolidBrush(Color.FromArgb(55, 58, 62)))
            using (Pen chBorder = new Pen(Color.FromArgb(35, 38, 42), 1.8f))
            {
                g.FillRectangle(chBrush, ch1X, ch1Y, 14, 45);
                g.DrawRectangle(chBorder, ch1X, ch1Y, 14, 45);
                g.FillRectangle(chBrush, ch2X, ch2Y, 12, 32);
                g.DrawRectangle(chBorder, ch2X, ch2Y, 12, 32);
            }

            // Chimney red/white band at top
            using (SolidBrush bandRed = new SolidBrush(Color.FromArgb(200, 50, 40)))
            using (SolidBrush bandWhite = new SolidBrush(Color.FromArgb(220, 220, 220)))
            {
                g.FillRectangle(bandRed, ch1X, ch1Y, 14, 4);
                g.FillRectangle(bandWhite, ch1X, ch1Y + 4, 14, 3);
                g.FillRectangle(bandRed, ch2X, ch2Y, 12, 3);
                g.FillRectangle(bandWhite, ch2X, ch2Y + 3, 12, 3);
            }

            // Blinking red warning lights on chimney tops
            float lightBlink = (float)(Math.Sin(blinkTimer * 4f) * 0.5 + 0.5);
            int blinkAlpha = (int)(lightBlink * 255);
            using (SolidBrush warningLight = new SolidBrush(Color.FromArgb(blinkAlpha, 255, 20, 20)))
            {
                g.FillEllipse(warningLight, ch1X + 4, ch1Y - 5, 6, 6);
                g.FillEllipse(warningLight, ch2X + 3, ch2Y - 5, 5, 5);
            }
            // Light glow
            using (SolidBrush lightGlow = new SolidBrush(Color.FromArgb(blinkAlpha / 4, 255, 60, 40)))
            {
                g.FillEllipse(lightGlow, ch1X, ch1Y - 10, 14, 14);
                g.FillEllipse(lightGlow, ch2X - 1, ch2Y - 9, 13, 13);
            }

            // Electrostatic filter rings
            UpgradeItem filterUp = FindUpgrade(model, "filter");
            if (filterUp != null && filterUp.Level > 0)
            {
                using (Pen filterPen = new Pen(Color.FromArgb(70, 190, 255), 2.5f))
                {
                    g.DrawRectangle(filterPen, ch1X - 2, ch1Y + 8, 18, 10);
                    g.DrawRectangle(filterPen, ch2X - 2, ch2Y + 6, 16, 8);
                }
                // Filter glow
                float filterGlow = (float)(Math.Sin(pulsePhase * 2f) * 20 + 30);
                using (SolidBrush fgBrush = new SolidBrush(Color.FromArgb((int)filterGlow, 70, 190, 255)))
                {
                    g.FillRectangle(fgBrush, ch1X - 3, ch1Y + 7, 20, 12);
                    g.FillRectangle(fgBrush, ch2X - 3, ch2Y + 5, 18, 10);
                }
            }

            // Facility Signboard on factory
            using (SolidBrush signBg2 = new SolidBrush(Color.FromArgb(30, 32, 36)))
            using (Pen signBorder = new Pen(Color.FromArgb(243, 156, 18), 1.5f))
            using (Font signFont2 = new Font("Segoe UI", 7.5f, FontStyle.Bold))
            using (SolidBrush signText2 = new SolidBrush(Color.FromArgb(243, 156, 18)))
            {
                g.FillRectangle(signBg2, plantX + 10, plantY + 4, 95, 14);
                g.DrawRectangle(signBorder, plantX + 10, plantY + 4, 95, 14);
                string signStr = string.Format("KARBON-9 MADEN\u0130");
                g.DrawString(signStr, signFont2, signText2, plantX + 14, plantY + 4);
            }
        }

        // =====================================================
        // 10. CHIMNEY HEAT SHIMMER
        // =====================================================
        private void DrawHeatShimmer(Graphics g, GameModel model)
        {
            if (model.CoalProductionSlider < 0.3) return;

            float plantY = 220f - 65f;
            float ch1X = 438f;
            float ch1Y = plantY - 45f;
            float ch2X = 464f;
            float ch2Y = plantY - 32f;

            int shimmerAlpha = (int)(model.CoalProductionSlider * 35);
            using (Pen shimmerPen = new Pen(Color.FromArgb(shimmerAlpha, 200, 190, 160), 1.2f))
            {
                // Wavy lines above chimney 1
                for (int s = 0; s < 5; s++)
                {
                    float sy = ch1Y - 8 - s * 10;
                    float sx = ch1X + 7;
                    float wave = (float)Math.Sin(pulsePhase * 3f + s * 1.4f) * 4f;
                    g.DrawLine(shimmerPen, sx + wave - 4, sy, sx + wave + 4, sy - 3);
                }
                // Wavy lines above chimney 2
                for (int s = 0; s < 4; s++)
                {
                    float sy = ch2Y - 6 - s * 9;
                    float sx = ch2X + 6;
                    float wave = (float)Math.Sin(pulsePhase * 3f + s * 1.6f + 1f) * 3.5f;
                    g.DrawLine(shimmerPen, sx + wave - 3, sy, sx + wave + 3, sy - 3);
                }
            }
        }

        // =====================================================
        // 11. UNDERGROUND SECTION - strata, arched tunnel,
        //     ore veins, dripping water, better lighting
        // =====================================================
        private void DrawUndergroundSection(Graphics g, int width, int height, GameModel model)
        {
            float groundY = 220f;
            float undergroundH = height - groundY;

            // === Strata Layers ===
            // Layer 1: Topsoil (brown)
            using (SolidBrush topsoil = new SolidBrush(Color.FromArgb(92, 64, 51)))
            {
                g.FillRectangle(topsoil, 0, groundY, width, 10);
            }
            // Layer 2: Clay (reddish-brown)
            using (SolidBrush clay = new SolidBrush(Color.FromArgb(115, 72, 48)))
            {
                g.FillRectangle(clay, 0, groundY + 10, width, 10);
            }
            // Layer 3: Sandstone (tan/yellow-brown)
            using (SolidBrush sandstone = new SolidBrush(Color.FromArgb(85, 78, 58)))
            {
                g.FillRectangle(sandstone, 0, groundY + 20, width, 12);
            }
            // Layer 4: Deep rock (dark gray)
            using (SolidBrush deepRock = new SolidBrush(Color.FromArgb(45, 42, 40)))
            {
                g.FillRectangle(deepRock, 0, groundY + 32, width, undergroundH - 32);
            }

            // Strata transition lines
            using (Pen strataLine = new Pen(Color.FromArgb(30, 0, 0, 0), 0.5f))
            {
                g.DrawLine(strataLine, 0, groundY + 10, width, groundY + 10);
                g.DrawLine(strataLine, 0, groundY + 20, width, groundY + 20);
                g.DrawLine(strataLine, 0, groundY + 32, width, groundY + 32);
            }

            // === Coal Seams ===
            using (SolidBrush coalVein = new SolidBrush(Color.FromArgb(16, 16, 18)))
            {
                g.FillRectangle(coalVein, 0, groundY + 45, 110, 35);
                g.FillRectangle(coalVein, 340, groundY + 50, 220, 40);
                g.FillRectangle(coalVein, 720, groundY + 40, width - 720, 50);
            }

            // Coal glints
            using (SolidBrush glintBrush = new SolidBrush(Color.FromArgb(75, 75, 80)))
            {
                g.FillRectangle(glintBrush, 45, groundY + 55, 8, 4);
                g.FillRectangle(glintBrush, 390, groundY + 65, 12, 5);
                g.FillRectangle(glintBrush, 480, groundY + 58, 7, 4);
                g.FillRectangle(glintBrush, 760, groundY + 52, 9, 4);
            }

            // === Glowing Ore Veins (yellow/gold dots in rock) ===
            Random oreRng = new Random(123);
            float oreGlow = (float)(Math.Sin(pulsePhase * 1.5f) * 30 + 60);
            using (SolidBrush oreBrush = new SolidBrush(Color.FromArgb((int)oreGlow, 220, 190, 60)))
            {
                for (int ov = 0; ov < 20; ov++)
                {
                    float ox = (float)(oreRng.NextDouble() * width);
                    float oy = groundY + 35f + (float)(oreRng.NextDouble() * (undergroundH - 40));
                    if (oy < groundY + 32) continue;
                    float osz = (float)(oreRng.NextDouble() * 3f + 1.5f);
                    g.FillEllipse(oreBrush, ox, oy, osz, osz);
                }
            }

            // === Mining Tunnel (Arched top) ===
            float tunnelY = groundY + 42f;
            float tunnelH = 50f;
            float tunnelLeft = 80f;
            float tunnelRight = 420f;
            float tunnelW = tunnelRight - tunnelLeft;

            // Tunnel background (dark)
            using (SolidBrush tunnelBg = new SolidBrush(Color.FromArgb(22, 20, 19)))
            {
                // Rectangular lower part
                g.FillRectangle(tunnelBg, tunnelLeft, tunnelY + 10, tunnelW, tunnelH - 10);
                // Arched top
                g.FillPie(tunnelBg, tunnelLeft, tunnelY - 15, tunnelW, 50, 180, 180);
            }

            // Mine Shaft (Vertical)
            using (SolidBrush shaftBg = new SolidBrush(Color.FromArgb(18, 17, 16)))
            {
                g.FillRectangle(shaftBg, 138, groundY, 30, tunnelY + tunnelH - groundY);
            }

            // Timber support beams (arched)
            using (Pen timberPen = new Pen(Color.FromArgb(120, 85, 55), 3f))
            {
                for (int t = 95; t < 410; t += 40)
                {
                    // Vertical posts
                    g.DrawLine(timberPen, (float)t, tunnelY + 8, (float)t, tunnelY + tunnelH);
                    // Arched cross beam
                    g.DrawArc(timberPen, (float)t - 18, tunnelY - 2, 36, 22, 180, 180);
                }
            }

            // Rails for mine cart
            float railY = tunnelY + tunnelH - 4;
            using (Pen railPen = new Pen(Color.FromArgb(130, 135, 140), 2f))
            {
                g.DrawLine(railPen, 80, railY, 410, railY);
                // Ties
                for (int sl = 85; sl < 410; sl += 14)
                {
                    g.DrawLine(railPen, (float)sl, railY - 2, (float)sl, railY + 2);
                }
            }

            // Animated Mine Cart
            DrawMineCart(g, cartPositionX, railY - 14, 26f, 14f, cartDirection > 0);

            // Miner worker with improved headlamp
            DrawMinerWorker(g, 390f, tunnelY + tunnelH - 24f);

            // === Dripping Water ===
            if (dripX != null)
            {
                using (SolidBrush dripBrush = new SolidBrush(Color.FromArgb(140, 80, 160, 230)))
                {
                    for (int d = 0; d < dripX.Length; d++)
                    {
                        float dx = dripX[d];
                        float dy = tunnelY + 2 + dripY[d] * (tunnelH - 8);
                        g.FillEllipse(dripBrush, dx, dy, 2.5f, 4f);
                    }
                }
                // Small splash at bottom
                using (SolidBrush splashBrush = new SolidBrush(Color.FromArgb(60, 80, 160, 230)))
                {
                    for (int d = 0; d < dripX.Length; d++)
                    {
                        if (dripY[d] > 0.9f)
                        {
                            g.FillEllipse(splashBrush, dripX[d] - 2, tunnelY + tunnelH - 6, 6, 3);
                        }
                    }
                }
            }

            // Methane Pocket Indicator
            UpgradeItem methaneUp = FindUpgrade(model, "methane");
            if (methaneUp == null || methaneUp.Level == 0)
            {
                float methAlpha = (float)(Math.Sin(pulsePhase * 4.0) * 35 + 40);
                using (SolidBrush methGlow = new SolidBrush(Color.FromArgb((int)methAlpha, 230, 80, 40)))
                using (Font methFont = new Font("Segoe UI", 6.5f, FontStyle.Bold))
                using (SolidBrush methTxt = new SolidBrush(Color.FromArgb(230, 100, 50)))
                {
                    g.FillEllipse(methGlow, 345, tunnelY - 12, 38, 22);
                    string methMsg = "CH\u2084 SIZINTI!";
                    g.DrawString(methMsg, methFont, methTxt, 340, tunnelY - 10);
                }
            }
            else
            {
                using (Font methFont = new Font("Segoe UI", 6.5f, FontStyle.Bold))
                using (SolidBrush methTxt = new SolidBrush(Color.FromArgb(80, 220, 120)))
                {
                    string methMsg = "\u26A1 CH\u2084 KOJENERASYON AKT\u0130F";
                    g.DrawString(methMsg, methFont, methTxt, 320, tunnelY - 12);
                }
            }
        }

        private void DrawMineCart(Graphics g, float x, float y, float w, float h, bool movingRight)
        {
            using (SolidBrush cartBrush = new SolidBrush(Color.FromArgb(85, 65, 55)))
            using (Pen cartPen = new Pen(Color.FromArgb(50, 40, 35), 1.5f))
            {
                PointF[] cartPoly = new PointF[]
                {
                    new PointF(x, y),
                    new PointF(x + w, y),
                    new PointF(x + w - 3, y + h),
                    new PointF(x + 3, y + h)
                };
                g.FillPolygon(cartBrush, cartPoly);
                g.DrawPolygon(cartPen, cartPoly);
            }

            // Coal mound
            using (SolidBrush coalHeap = new SolidBrush(Color.FromArgb(20, 20, 22)))
            {
                g.FillPie(coalHeap, x + 2, y - 6, w - 4, 12, 180, 180);
            }

            // Wheels
            using (SolidBrush wheelBrush = new SolidBrush(Color.FromArgb(30, 30, 35)))
            using (Pen wheelPen = new Pen(Color.FromArgb(160, 165, 170), 1f))
            {
                g.FillEllipse(wheelBrush, x + 4, y + h - 2, 6, 6);
                g.DrawEllipse(wheelPen, x + 4, y + h - 2, 6, 6);
                g.FillEllipse(wheelBrush, x + w - 10, y + h - 2, 6, 6);
                g.DrawEllipse(wheelPen, x + w - 10, y + h - 2, 6, 6);
            }
        }

        private void DrawMinerWorker(Graphics g, float x, float y)
        {
            // Body (Overalls)
            using (SolidBrush clothBrush = new SolidBrush(Color.FromArgb(40, 70, 110)))
            {
                g.FillRectangle(clothBrush, x, y + 8, 8, 14);
            }

            // Head & Yellow Hardhat
            using (SolidBrush faceBrush = new SolidBrush(Color.FromArgb(240, 190, 150)))
            using (SolidBrush helmetBrush = new SolidBrush(Color.FromArgb(241, 196, 15)))
            {
                g.FillEllipse(faceBrush, x + 1, y + 2, 6, 6);
                g.FillRectangle(helmetBrush, x, y, 8, 4);
            }

            // Headlamp LED (bright)
            using (SolidBrush lampDot = new SolidBrush(Color.FromArgb(255, 255, 220)))
            {
                g.FillEllipse(lampDot, x + 7, y + 1, 3, 3);
            }

            // Wider headlamp beam with gradient feel
            using (SolidBrush lampBeamInner = new SolidBrush(Color.FromArgb(55, 255, 255, 200)))
            {
                PointF[] beamInner = new PointF[]
                {
                    new PointF(x + 9, y + 2),
                    new PointF(x + 50, y - 4),
                    new PointF(x + 50, y + 14)
                };
                g.FillPolygon(lampBeamInner, beamInner);
            }
            using (SolidBrush lampBeamOuter = new SolidBrush(Color.FromArgb(25, 255, 255, 160)))
            {
                PointF[] beamOuter = new PointF[]
                {
                    new PointF(x + 9, y + 2),
                    new PointF(x + 65, y - 10),
                    new PointF(x + 65, y + 20)
                };
                g.FillPolygon(lampBeamOuter, beamOuter);
            }

            // Pickaxe
            using (Pen pickPen = new Pen(Color.FromArgb(160, 140, 110), 1.5f))
            {
                g.DrawLine(pickPen, x + 6, y + 12, x + 18, y + 6);
                g.DrawLine(pickPen, x + 16, y + 5, x + 22, y + 8);
            }
        }

        // =====================================================
        // 12. SMOKE PARTICLES - improved transparency, growth
        // =====================================================
        private void DrawSmokeParticles(Graphics g)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                SmokeParticle p = particles[i];
                int baseAlpha = (int)Math.Max(0, Math.Min(255, p.Alpha));

                // Growth ratio determines transparency variation
                float growthRatio = 1f;
                if (p.MaxRadius > p.InitialRadius)
                {
                    growthRatio = (p.Radius - p.InitialRadius) / (p.MaxRadius - p.InitialRadius);
                }
                growthRatio = Math.Max(0f, Math.Min(1f, growthRatio));

                // Outer puff (lighter, more transparent)
                int outerAlpha = (int)(baseAlpha * 0.35f);
                float outerR = p.Radius * 1.3f;
                using (SolidBrush outerBrush = new SolidBrush(Color.FromArgb(outerAlpha, p.BaseColor)))
                {
                    g.FillEllipse(outerBrush, p.X - outerR, p.Y - outerR, outerR * 2, outerR * 2);
                }

                // Inner core (denser)
                int innerAlpha = (int)(baseAlpha * 0.8f);
                float innerR = p.Radius * 0.7f;
                // Slightly different shade for core
                int coreR = Math.Min(255, p.BaseColor.R + 15);
                int coreG = Math.Min(255, p.BaseColor.G + 15);
                int coreB = Math.Min(255, p.BaseColor.B + 15);
                using (SolidBrush innerBrush = new SolidBrush(Color.FromArgb(innerAlpha, coreR, coreG, coreB)))
                {
                    g.FillEllipse(innerBrush, p.X - innerR, p.Y - innerR, innerR * 2, innerR * 2);
                }
            }
        }

        // =====================================================
        // 13. SEVERE WEATHER
        // =====================================================
        private void DrawSevereWeatherAlert(Graphics g, int width, int height, GameModel model)
        {
            // Acid rain overlay
            using (Pen rainPen = new Pen(Color.FromArgb(70, 180, 200, 140), 1f))
            {
                for (int r = 0; r < 25; r++)
                {
                    float rx = (float)(rnd.NextDouble() * width);
                    float ry = (float)(rnd.NextDouble() * height);
                    g.DrawLine(rainPen, rx, ry, rx + 4, ry + 12);
                }
            }

            // Flashing warning badge
            if ((int)(pulsePhase * 2.5) % 2 == 0)
            {
                using (SolidBrush warnBg = new SolidBrush(Color.FromArgb(200, 180, 30, 30)))
                using (Font warnFont = new Font("Segoe UI", 9f, FontStyle.Bold))
                using (SolidBrush warnTxt = new SolidBrush(Color.White))
                {
                    string alertMsg = string.Format(
                        "\u26A0\uFE0F AS\u0130T YA\u011EMURU & A\u015EIRI SICAK UYARISI (+{0:0.0}\u00B0C)",
                        model.GlobalWarmingTemp);
                    g.FillRectangle(warnBg, width / 2 - 170, 12, 340, 26);
                    g.DrawString(alertMsg, warnFont, warnTxt, width / 2 - 160, 16);
                }
            }
        }

        // =====================================================
        // 13.5 CLEAN VICTORY RAINBOW & SEASONAL WEATHER
        // =====================================================
        private void DrawCleanVictoryRainbow(Graphics g, int width, int height)
        {
            Color[] rainbowColors = new Color[] {
                Color.FromArgb(45, 231, 76, 60),
                Color.FromArgb(45, 230, 126, 34),
                Color.FromArgb(45, 241, 196, 15),
                Color.FromArgb(45, 46, 204, 113),
                Color.FromArgb(45, 52, 152, 219),
                Color.FromArgb(45, 155, 89, 182)
            };

            for (int r = 0; r < rainbowColors.Length; r++)
            {
                using (Pen rPen = new Pen(rainbowColors[r], 4.5f))
                {
                    g.DrawArc(rPen, width / 2 - 280, 25 + r * 4, 560, 240, 190, 160);
                }
            }
        }

        private void DrawFallingSnow(Graphics g, int width, int height)
        {
            if (snowX == null) return;
            using (SolidBrush snowBrush = new SolidBrush(Color.FromArgb(215, 255, 255, 255)))
            {
                for (int i = 0; i < snowX.Length; i++)
                {
                    float sz = snowSize[i];
                    g.FillEllipse(snowBrush, snowX[i], snowY[i], sz, sz);
                }
            }
        }

        private void DrawFallingAutumnLeaves(Graphics g, int width, int height)
        {
            if (leafX == null) return;
            for (int i = 0; i < leafX.Length; i++)
            {
                Color c = leafColors[i % leafColors.Length];
                using (SolidBrush leafBrush = new SolidBrush(Color.FromArgb(200, c)))
                {
                    g.FillEllipse(leafBrush, leafX[i], leafY[i], 5.5f, 3.5f);
                }
            }
        }

        // =====================================================
        // 14. FLOATING TEXTS
        // =====================================================
        private void DrawFloatingTexts(Graphics g)
        {
            using (Font ftFont = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            {
                for (int i = 0; i < floatingTexts.Count; i++)
                {
                    FloatingText ft = floatingTexts[i];
                    int a = (int)Math.Max(0, Math.Min(255, ft.Alpha));

                    // Drop shadow
                    using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(a / 3, 0, 0, 0)))
                    {
                        g.DrawString(ft.Text, ftFont, shadowBrush, ft.X + 1, ft.Y + 1);
                    }

                    using (SolidBrush ftBrush = new SolidBrush(Color.FromArgb(a, ft.TextColor)))
                    {
                        g.DrawString(ft.Text, ftFont, ftBrush, ft.X, ft.Y);
                    }
                }
            }
        }

        // =====================================================
        // UTILITY
        // =====================================================
        private Color InterpolateColor(Color c1, Color c2, float t)
        {
            t = Math.Max(0f, Math.Min(1f, t));
            int r = (int)(c1.R + (c2.R - c1.R) * t);
            int gr = (int)(c1.G + (c2.G - c1.G) * t);
            int b = (int)(c1.B + (c2.B - c1.B) * t);
            return Color.FromArgb(r, gr, b);
        }
    }
}
