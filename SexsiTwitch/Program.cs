
using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace SexsiTwitch
{
    static class Program
    {
        private static void Main()
        {
            Loading.OnLoadingComplete += Loaded;
        }

        private static CheckBox DrawEDamage,
            DrawReady,
            DrawW,
            DrawE,
            DrawR,
            ComboQOption,
            ComboWOption,
            EEnemyLeaving,
            HarassW,
            EJungleKS,
            QRecall,
            WUnderTurret,
            SaveManaForE,
            DrawQTime,
            DrawRTime,
            DrawEStacks,
            DrawEStacksTime;

        private static Slider QInRange, ComboECustomStacks, HarassWMana;

        private static ComboBox ComboETypeOption, ComboR, HarassE;

        private static Color DrawColor = Color.FromArgb(255, 255, 0, 255);

        private static Spell.Skillshot W;

        private static Spell.Active Q;
        public static Spell.Active E;
        private static Spell.Active R;

        private static AIHeroClient Hero => Player.Instance;

        private static float rRange = 850f + Hero.BoundingRadius;

        private static void Loaded(EventArgs args)
        {
            if (Player.Instance.Hero == Champion.Twitch)
            {
                Q = new Spell.Active(SpellSlot.Q);
                W = new Spell.Skillshot(SpellSlot.W, 950, SkillShotType.Circular, 250, 1400, 280)
                {
                    MinimumHitChance = HitChance.Medium
                };
                E = new Spell.Active(SpellSlot.E, 1200);
                R = new Spell.Active(SpellSlot.R);

                ComboWType = new[] { "Instant", "Smart Logic", "Off" };
                ComboEType = new[] { "Kill Only", "Custom Stacks", "Off" };
                ComboRType = new[] { "Off", "1", "2" , "3" , "4" , "5" };
                HarassEType = new[] { "Off", "1", "2" , "3" , "4" , "5", "6" };

                var TwitchMenu = MainMenu.AddMenu("SexsiTwitch", "mikoTwitch");

                var DrawMenu = TwitchMenu.AddSubMenu("Drawings");
                DrawReady = DrawMenu.Add("DrawReady", new CheckBox("Draw Only Ready Spells"));
                DrawW = DrawMenu.Add("DrawW", new CheckBox("Draw W Range"));
                DrawE = DrawMenu.Add("DrawE", new CheckBox("Draw E Range"));
                DrawR = DrawMenu.Add("DrawR", new CheckBox("Draw R Range"));
                DrawEDamage = DrawMenu.Add("DrawEDamage", new CheckBox("Draw E Damage"));
                DrawQTime = DrawMenu.Add("DrawQTime", new CheckBox("Draw Q Time"));
                DrawEStacks = DrawMenu.Add("DrawEStacks", new CheckBox("Draw E Stacks"));
                DrawEStacksTime = DrawMenu.Add("DrawEStacksTime", new CheckBox("Draw E Stack Time"));
                DrawRTime = DrawMenu.Add("DrawRTime", new CheckBox("Draw R Time"));

                //COMBO MENU
                var ComboMenu = TwitchMenu.AddSubMenu("Combo");
                ComboQOption = ComboMenu.Add("ComboQOption", new CheckBox("Use Q in Combo"));
                QInRange = ComboMenu.Add("QInRange", new Slider("-> Use Q if Enemy in X Range", 600, 0, 3000));
                ComboWOption = ComboMenu.Add("ComboWOption", new CheckBox("Use W in Combo"));
                ComboETypeOption = ComboMenu.Add("ComboETypeOption", new ComboBox("Use E in Combo", 0, ComboEType));
                ComboECustomStacks = ComboMenu.Add("ComboECustomStacks",
                    new Slider("-> Custom E Stacks", 6, 1, 6));
                EEnemyLeaving = ComboMenu.Add("EEnemyLeaving", new CheckBox("Use E if Enemy Escaping Range"));
                ComboR = ComboMenu.Add("ComboR", new ComboBox("Use R if X Enemies in Range", 0, ComboRType));

                //HARASS MENU
                var HarassMenu = TwitchMenu.AddSubMenu("Harass");
                HarassW = HarassMenu.Add("HarassW", new CheckBox("Use W for Harass", false));
                HarassWMana = HarassMenu.Add("HarassWMana", new Slider("-> Min Mana % W Harass", 75));
                HarassE = HarassMenu.Add("HarassE", new ComboBox("Use E Harass on X Stacks", 0, HarassEType));

                //LANE CLEAR MENU
                var LaneClearMenu = TwitchMenu.AddSubMenu("Jungle Clear");
                EJungleKS = LaneClearMenu.Add("EJungleKS", new CheckBox("Use E to KS Jungle Mobs"));

                //EXTRA MENU
                var ExtraMenu = TwitchMenu.AddSubMenu("Extra Settings");
                QRecall = ExtraMenu.Add("QRecall", new CheckBox("Stealth Recall"));
                WUnderTurret = ExtraMenu.Add("WUnderTurret", new CheckBox("Use W if Under Enemy Turret", false));
                SaveManaForE = ExtraMenu.Add("SaveManaForE", new CheckBox("Save Mana for E"));

                PassiveManager.Init();

                Drawing.OnEndScene += Drawing_OnEndScene;
                Game.OnUpdate += Game_OnUpdate;
                Spellbook.OnCastSpell += Spellbook_OnCastSpell;
                Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnSpellCast;
                Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
                Drawing.OnDraw += Drawing_OnDraw;
            }
            else
            {
                Chat.Print("CHAMPION NOT SUPPORTED");
            }
        }

        private static float GetRemainingBuffTime(this Obj_AI_Base target, string buffName)
        {
            return
                target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => string.Equals(buff.Name, buffName, StringComparison.CurrentCultureIgnoreCase))
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault() - Game.Time;
        }

        private static void DrawTextOnScreen(this Vector3 location, string message, Color colour)
        {
            var worldToScreen = Drawing.WorldToScreen(location);
            Drawing.DrawText(worldToScreen[0] - message.Length * 5, worldToScreen[1] - 200, colour, message);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (DrawReady.CurrentValue)
            {
                if (W.IsReady() && DrawW.CurrentValue)
                {
                    Circle.Draw(new ColorBGRA(DrawColor.ToArgb()), W.Range, Hero);
                }
                if (E.IsReady() && DrawE.CurrentValue)
                {
                    Circle.Draw(new ColorBGRA(DrawColor.ToArgb()), E.Range, Hero);
                }
                if (R.IsReady() && DrawR.CurrentValue)
                {
                    Circle.Draw(new ColorBGRA(DrawColor.ToArgb()), rRange, Hero);
                }
            }
            else
            {
                if (DrawW.CurrentValue)
                {
                    Circle.Draw(new ColorBGRA(DrawColor.ToArgb()), W.Range, Hero);
                }
                if (DrawE.CurrentValue)
                {
                    Circle.Draw(new ColorBGRA(DrawColor.ToArgb()), E.Range, Hero);
                }
                if (DrawR.CurrentValue)
                {
                    Circle.Draw(new ColorBGRA(DrawColor.ToArgb()), rRange, Hero);
                }
            }

            if (DrawQTime.CurrentValue
                && Hero.HasBuff("TwitchHideInShadows"))
            {
                var position = new Vector3(
                    ObjectManager.Player.Position.X,
                    ObjectManager.Player.Position.Y - 30,
                    ObjectManager.Player.Position.Z);
                position.DrawTextOnScreen(
                    "Stealth:  " + $"{Hero.GetRemainingBuffTime("TwitchHideInShadows"):0.0}",
                    Color.AntiqueWhite);
            }

            if (DrawRTime.CurrentValue
                && Hero.HasBuff("TwitchFullAutomatic"))
            {
                Hero.Position.DrawTextOnScreen(
                    "Ultimate:  " + $"{Hero.GetRemainingBuffTime("TwitchFullAutomatic"):0.0}",
                    Color.AntiqueWhite);
            }

            if (DrawEStacks.CurrentValue)
                foreach (var source in
                    EntityManager.Heroes.Enemies.Where(x => x.HasBuff("twitchdeadlyvenom") && !x.IsDead && x.IsVisible))
                {
                    var position = new Vector3(source.Position.X, source.Position.Y + 10, source.Position.Z);
                    position.DrawTextOnScreen($"{"Stacks: " + source.PassiveCount()}", Color.AntiqueWhite);
                }

            if (DrawEStacksTime.CurrentValue)
                foreach (var source in
                    EntityManager.Heroes.Enemies.Where(x => x.HasBuff("twitchdeadlyvenom") && !x.IsDead && x.IsVisible))
                {
                    var position = new Vector3(source.Position.X, source.Position.Y - 30, source.Position.Z);
                    position.DrawTextOnScreen(
                        "Stack Timer:  " + $"{source.GetRemainingBuffTime("twitchdeadlyvenom"):0.0}",
                        Color.AntiqueWhite);
                }
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit Target, EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && ComboWOption.CurrentValue)
            {
                if (Target.IsValidTarget() && Target is AIHeroClient)
                {
                    if (SaveManaForE.CurrentValue && Hero.Mana - W.ManaCost < E.ManaCost)
                        return;

                    if (!WUnderTurret.CurrentValue && Hero.IsUnderEnemyturret())
                        return;

                    var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                    if (target == null) return;

                    if (W.Cast(target)) return;
                }
            }
        }

        private static void Obj_AI_Base_OnSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy && args.SData.Name == "SummonerFlash" && sender.HasBuff("twitchdeadlyvenom") && (Hero.Position - sender.Position).Length() < E.Range && (Hero.Position - args.End).Length() > E.Range)
            {
                if (E.Cast()) { return; }
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook spellbook, SpellbookCastSpellEventArgs args)
        {
            Hero.Buffs.ForEach(x => Console.WriteLine(x.Name));
            if (args.Slot == SpellSlot.Recall && QRecall.CurrentValue && Q.IsReady() && !Hero.HasBuff("globalcamouflage"))
            {   
                Q.Cast();
                args.Process = true;
                return;
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                Combo();
            else if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                LaneClear();

            if (EJungleKS.CurrentValue && (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) ||  Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.JungleClear)))
            {
                foreach (var JGCreep in EntityManager.MinionsAndMonsters.GetJungleMonsters())
                {
                    if (JGCreep.IsValidTarget(E.Range) && JGCreep.HasBuff("twitchdeadlyvenom") && CalcEDamage(JGCreep) > JGCreep.Health)
                    {
                        if (JGCreep.Name.Contains("Red") || JGCreep.Name.Contains("Blue") || JGCreep.Name.Contains("Dragon") || JGCreep.Name.Contains("Rift") || JGCreep.Name.Contains("Baron"))
                            if (E.Cast()) { return; }
                    }
                }
            }
        }

        private static void LaneClear()
        {
            foreach (var Enemy in EntityManager.Heroes.Enemies)
            {
                if (HarassE.CurrentValue > 0 && Enemy.IsValidTarget() && !Enemy.IsZombie)
                {
                    var flDistance = (Hero.Position - Enemy.Position).Length();

                    if (Enemy.HasBuff("twitchdeadlyvenom"))
                    {
                        if (flDistance <= E.Range) // Use E in Harass
                        {
                            if (Enemy.GetBuffCount("twitchdeadlyvenom") >= HarassE.CurrentValue || CalcEDamage(Enemy) > Enemy.TotalShieldHealth())
                                if (E.Cast()) { return; }
                            if (EEnemyLeaving.CurrentValue && flDistance > E.Range - 20 && !Enemy.IsFacing(Hero))
                                if (E.Cast()) { return; }
                        }
                    }
                }
            }

            if (SaveManaForE.CurrentValue && Hero.Mana - W.ManaCost < E.ManaCost)
                return;
            if (!WUnderTurret.CurrentValue && Hero.IsUnderEnemyturret())
                return;

            if (HarassW.CurrentValue && Hero.ManaPercent > HarassWMana.CurrentValue)
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                if (target == null) return;

                if (W.Cast(target)) return;
            }
        }

        private static void Combo()
        {
            if (ComboR.CurrentValue > 0 && Hero.CountEnemyChampionsInRange(rRange) >= ComboR.CurrentValue)
                if (R.Cast()) return;

            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (enemy.IsValidTarget() && !enemy.IsZombie)
                {
                    var flDistance = (Hero.Position - enemy.Position).Length();

                    if (enemy.HasBuff("twitchdeadlyvenom"))
                    {
                        if (flDistance <= E.Range) // Use E in Combo
                        {
                            if (ComboETypeOption.CurrentValue == 1 && (enemy.GetBuffCount("twitchdeadlyvenom") >= ComboECustomStacks.CurrentValue || CalcEDamage(enemy) > enemy.TotalShieldHealth()))
                                if (E.Cast()) { return; }
                            if (ComboETypeOption.CurrentValue == 0 && CalcEDamage(enemy) > enemy.TotalShieldHealth())
                                if (E.Cast()) { return; }
                            if (EEnemyLeaving.CurrentValue && flDistance > E.Range - 20 && !enemy.IsFacing(Hero))
                                if (E.Cast()) { return; }
                        }
                    }

                    if (ComboQOption.CurrentValue && Q.IsReady() && flDistance < QInRange.CurrentValue)
                    {
                        if (SaveManaForE.CurrentValue && Hero.Mana - Q.ManaCost < E.ManaCost)
                            continue;
                        if (Q.Cast()) { return; }
                    }
                }
            }
            if (Hero.CountEnemyChampionsInRange(Hero.AttackRange + Hero.BoundingRadius) == 0 && ComboWOption.CurrentValue && !Hero.HasBuff("globalcamouflage") && !Hero.HasBuff("twitchhideinshadowsbuff"))
            {
                if (SaveManaForE.CurrentValue && Hero.Mana - W.ManaCost < E.ManaCost)
                    return;

                if (!WUnderTurret.CurrentValue && Hero.IsUnderEnemyturret())
                    return;

                var target = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                if(target == null) return;

                if (W.Cast(target)) return;
            }
        }

        private static string[] ComboWType, ComboEType, ComboRType, HarassEType;

        private static void Drawing_OnEndScene(System.EventArgs args)
        {
            if (DrawEDamage.CurrentValue && E.IsReady())
            {
                foreach (var enemy in EntityManager.Heroes.Enemies)
                {
                    if (enemy.IsHPBarRendered && enemy.VisibleOnScreen && CalcEDamage(enemy) > 0f)
                    {
                        var vector16 = new Vector2(2f, 10f + 9f / 2f);
                        var num33 = CalcEDamage(enemy);
                        var num26 = enemy.MaxHealth + enemy.AllShield + enemy.AttackShield + enemy.MagicShield;
                        var num27 = (int)enemy.HPBarPosition.X + (int)vector16.X;
                        var num28 = (int)enemy.HPBarPosition.Y + (int)vector16.Y - 5;
                        var num34 = enemy.Health;
                        var num35 = (num34 - num33 > 0f ? num34 - num33 : 0f) / num26;
                        var num36 = num34 / num26;
                        Vector2 vector19 = new Vector2(num27 + num35 * 104f, num28);
                        Vector2 vector20 = new Vector2(num27 + num36 * 104f + 1, num28);
                        Console.WriteLine($"E Damage: {num33}");
                        Line.DrawLine(Color.Yellow, 9f, vector19, vector20);
                        Drawing.DrawText(vector19 - new Vector2(2, 25), Color.FromArgb(255, 255, 255, 255), "E", 3);
                    }
                }
            }
        }

        private static float CalcEDamage(Obj_AI_Base Target)
        {
            float result;
            if (!Target.HasBuff("twitchdeadlyvenom"))
            {
                result = 0f;
            }
            else
            {
                var objGeneralParticleEmitter = ObjectManager.Get<Obj_GeneralParticleEmitter>().FirstOrDefault(a => a.IsValid && a.Name.Contains("Twitch_Base_P_Stack"));
                if (objGeneralParticleEmitter != null)
                {
                    Console.WriteLine(objGeneralParticleEmitter.Name);
                    int pasifStack = 0;
                    string name = objGeneralParticleEmitter.Name;
                    if (name.Contains("1"))
                    {
                        pasifStack = 1;
                    }
                    else if (name.Contains("2"))
                    {
                        pasifStack = 2;
                    }
                    else if (name.Contains("3"))
                    {
                        pasifStack = 3;
                    }
                    else if (name.Contains("4"))
                    {
                        pasifStack = 4;
                    }
                    else if (name.Contains("5"))
                    {
                        pasifStack = 5;
                    }
                    else if (name.Contains("6"))
                    {
                        pasifStack = 6;
                    }
                    if (pasifStack > 0)
                    {
                        int Elevel = E.Level - 1;
                        float eDamage = (float)(new[]
                        {
                            20f,
                            35f,
                            50f,
                            65f,
                            80f
                        }[Elevel] + pasifStack * (new[]
                        {
                            15f,
                            20f,
                            25f,
                            30f,
                            35f
                        }[Elevel] + 0.2 * Hero.TotalMagicalDamage + 0.25 * Hero.FlatPhysicalDamageMod));
                        return Hero.CalculateDamageOnUnit(Target, 0, CheckBuff(eDamage, Target));
                    }
                }
                result = 0f;
            }
            return result;
        }

        private static float CheckBuff(float dmg, Obj_AI_Base target)
        {
            if (Hero.HasBuff("summonerexhaust"))
            {
                dmg *= 0.6f;
            }
            if (target.HasBuff("ferocioushowl"))
            {
                dmg *= 0.7f;
            }
            if (target.BaseSkinName == "Moredkaiser")
            {
                dmg -= target.Mana;
            }
            if (target.HasBuff("GarenW"))
            {
                dmg *= 0.7f;
            }
            return dmg;
        }
    }
}
