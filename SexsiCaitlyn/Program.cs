using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;

namespace SexsiCaitlyn
{
    internal class Caitlyn
    {
        private static AIHeroClient Hero => Player.Instance;

        private static Spell.Skillshot Q, W, E;

        private static CheckBox QInCombo, SafeQKS, WAfterE, TrapImmobileCombo, EWhenClose, RInCombo, EnemyToBlockR, SafeQHarass, LaneClearQ, EToMouse;

        private static ComboBox TrapEnemyCast, TrapEnemyCastHarass;

        private static KeyBind SemiManualEMenuKey, SemiManualMenuKey;

        private static Slider ShortQDisableLevel, EBeforeLevel, UltRange, SafeQHarassMana, LaneClearMana, WDelay;

        static Caitlyn()
        {
            var TrapEnemyCastType = new[] { "Exact Position", "Vector Extension", "Turn Off" };

            Q = new Spell.Skillshot(SpellSlot.Q, 1300, SkillShotType.Linear, (int)0.625, 2200, 90);
            W = new Spell.Skillshot(SpellSlot.W, 800, SkillShotType.Circular, (int)1.1, 3200, 100);
            E = new Spell.Skillshot(SpellSlot.E, 950, SkillShotType.Linear, (int)0.125, 1600, 90);

            ComboTrap = false;
            UseNet = false;
            UseNetCombo = false;

            LastTrapTime = Environment.TickCount;

            var CaitlynMenu = MainMenu.AddMenu("SexsiCaitlyn", "mikocaitlyn");

            var comboMenu = CaitlynMenu.AddSubMenu("++ Combo");
            QInCombo = comboMenu.Add("QInCombo", new CheckBox("Use Q in Combo"));
            SafeQKS = comboMenu.Add("SafeQKS", new CheckBox("Safe Q KS"));
            ShortQDisableLevel = comboMenu.Add("ShortQDisableLevel",
                new Slider("Disable Short-Q after level", 11, 0, 18));
            WAfterE = comboMenu.Add("WAfterE", new CheckBox("Use W in Burst Combo"));
            TrapEnemyCast = comboMenu.Add("TrapEnemyCast",
                new ComboBox("Use W on Enemy AA/Spellcast", 0, TrapEnemyCastType));
            TrapImmobileCombo = comboMenu.Add("TrapImmobileCombo", new CheckBox("Use W on Immobile Enemies"));
            EBeforeLevel = comboMenu.Add("EBeforeLevel", new Slider("Disable Long-E After Level", 18, 0, 18));
            EWhenClose = comboMenu.Add("EWhenClose", new CheckBox("Use E on Gapcloser/Close Enemy"));
            SemiManualEMenuKey = comboMenu.Add("SemiManualEMenuKey",
                new KeyBind("E Semi-Manual Key", false, KeyBind.BindTypes.HoldActive, 17));
            RInCombo = comboMenu.Add("RInCombo", new CheckBox("Use R in Combo"));
            SemiManualMenuKey = comboMenu.Add("SemiManualMenuKey",
                new KeyBind("R Semi-Manual Key", false, KeyBind.BindTypes.HoldActive, 84));
            UltRange = comboMenu.Add("UltRange", new Slider("Dont R if Enemies in Range", 1100, 0, 3000));
            EnemyToBlockR = comboMenu.Add("EnemyToBlockR", new CheckBox("Dont R if an Enemy Can Block", false));

            var harassMenu = CaitlynMenu.AddSubMenu("++ Harass");
            SafeQHarass = harassMenu.Add("SafeQHarass", new CheckBox("Use Q Smart Harass"));
            SafeQHarassMana = harassMenu.Add("SafeQHarassMana", new Slider("Q Harass Above Mana Percent", 60));
            TrapEnemyCastHarass = harassMenu.Add("TrapEnemyCastHarass",
                new ComboBox("Use W on Enemy AA/Spellcast", 0, TrapEnemyCastType));

            var laneclearMenu = CaitlynMenu.AddSubMenu("++ Lane Clear");
            LaneClearQ = laneclearMenu.Add("LaneClearQ", new CheckBox("Use Q to Laneclear"));
            LaneClearMana = laneclearMenu.Add("LaneClearMana", new Slider("Q Laneclear Above Mana Percent", 80));

            var extraMenu = CaitlynMenu.AddSubMenu("++ Extra Settings");
            WDelay = extraMenu.Add("WDelay", new Slider("Minimum Delay Between Traps (W)", 2, 0, 15));
            EToMouse = extraMenu.Add("EToMouse", new CheckBox("Enable E-to-Cursor", false));

            Game.OnUpdate += Game_OnUpdate;
            Obj_AI_Base.OnSpellCast += Obj_AI_Base_OnSpellCast;
            Orbwalker.OnPostAttack += Orbwalker_OnPostAttack;
        }

        private static void Orbwalker_OnPostAttack(AttackableUnit Target, EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && Target is AIHeroClient && Target.IsValidTarget())
            {
                if (Hero.Level <= EBeforeLevel.CurrentValue && E.CastMinimumHitchance((AIHeroClient)Target, 90) && E.GetPrediction((AIHeroClient)Target).HitChance != HitChance.Collision)
                {
                    UseNetCombo = true;
                    ComboTarget = (AIHeroClient)Target;
                    return;
                }
            }
        }

        private static void Obj_AI_Base_OnSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo) && TrapEnemyCast.CurrentValue < 2 && sender.IsEnemy && sender is AIHeroClient && sender.IsValidTarget(750))
            {
                if (Environment.TickCount - LastTrapTime > WDelay.CurrentValue)
                {
                    if (TrapEnemyCast.CurrentValue == 0)
                    {
                        if (W.Cast(sender.Position))
                        {
                            LastTrapTime = Environment.TickCount;
                            return;
                        }
                    }
                    else
                    {
                        var EndPosition = Hero.Position + (sender.Position - Hero.Position).Normalized() * ((sender.Position - Hero.Position).Length() + 50);
                        if (W.Cast(EndPosition))
                        {
                            LastTrapTime = Environment.TickCount;
                            return;
                        }
                    }
                }

            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit) && TrapEnemyCastHarass.CurrentValue < 2 && sender.IsEnemy && sender is AIHeroClient && sender.IsValidTarget(750))
            {
                if (Environment.TickCount - LastTrapTime > WDelay.CurrentValue)
                {
                    if (TrapEnemyCastHarass.CurrentValue == 0)
                    {
                        if (W.Cast(sender.Position))
                        {
                            LastTrapTime = Environment.TickCount;
                            return;
                        }
                    }
                    else
                    {
                        var EndPosition = Hero.Position + (sender.Position - Hero.Position).Normalized() * ((sender.Position - Hero.Position).Length() + 50);
                        if (W.Cast(EndPosition))
                        {
                            LastTrapTime = Environment.TickCount;
                            return;
                        }
                    }
                }

            }

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear) && sender.IsEnemy && sender is AIHeroClient && sender.IsValidTarget(1250))
            {
                if (SafeQHarass.CurrentValue && Hero.ManaPercent > SafeQHarassMana.CurrentValue && Hero.CountEnemyChampionsInRange(800) == 0)
                {
                    if (Q.Cast(sender.Position)) { return; }
                }
            }

            if (QInCombo.CurrentValue && args.SData.Name.Contains("CaitlynHeadshotMissile") && args.Target is AIHeroClient && args.Target.IsValid)
            {
                var flDistance = (args.Target.Position - Hero.Position).Length();
                if (flDistance < Q.Range)
                {
                    if (flDistance > 650 || Hero.Level < ShortQDisableLevel.CurrentValue)
                    {
                        var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

                        if (target == null) return;

                        if (Q.CastMinimumHitchance(target, 60)) { return; }
                    }
                }
            }

            if (WAfterE.CurrentValue && args.SData.Name.Contains("CaitlynEntrapment"))
            {
                if (ComboTarget != null && ComboTarget.IsValidTarget())
                {
                    var EstimatedEnemyPos = Prediction.Position.PredictUnitPosition(ComboTarget, (int)0.5);

                    if (W.Cast((Vector3)EstimatedEnemyPos)) { return; }
                }
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                Combo();

            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
                LaneClear();

            if (SemiManualMenuKey.CurrentValue)
            {
                var target = TargetSelector.GetTarget(new[] { 2000, 2500, 3000 }[Hero.Spellbook.GetSpell(SpellSlot.R).Level - 1],
                    DamageType.Physical);

                if (target == null) return;

                Player.CastSpell(SpellSlot.R, target);
            }

            if (SemiManualEMenuKey.CurrentValue)
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

                if (target == null) return;

                E.Cast(target);
            }

            if (E.IsReady() && UseNet && EToMouse.CurrentValue && !UseNetCombo)
            {
                var CastPosition = Hero.ServerPosition - (Game.CursorPos - Hero.ServerPosition);
                E.Cast(CastPosition);
            }

        }

        private static void LaneClear()
        {
            if (LaneClearQ.CurrentValue && Hero.ManaPercent > LaneClearMana.CurrentValue)
            {
                var Qfarm = EntityManager.MinionsAndMonsters.GetLaneMinions(
                    EntityManager.UnitTeam.Enemy,
                    Hero.Position,
                    Q.Range);

                if (Qfarm == null)
                    return;

                foreach (var objAiMinion in from objAiMinion in Qfarm
                    let rectangle = new Geometry.Polygon.Rectangle(Player.Instance.Position,
                        Player.Instance.Position.Extend(objAiMinion, Player.Instance.Distance(objAiMinion) > 900 ? 900 - Player.Instance.Distance(objAiMinion) : 900).To3D(), 20)
                    let count = Qfarm.Count(minion => new Geometry.Polygon.Circle(minion.Position, objAiMinion.BoundingRadius).Points.Any(rectangle.IsInside))
                    where count >= 3
                    select objAiMinion)
                {
                    Q.Cast(objAiMinion);
                }
            }
        }

        private static void Combo()
        {
            foreach (var Enemy in EntityManager.Heroes.Enemies)
            {
                var flDistance = (Enemy.Position - Hero.Position).Length();

                if (Enemy == null) return;

                if (Enemy.IsValidTarget())
                {
                    if (SafeQKS.CurrentValue && flDistance > 675 && Hero.CountEnemyChampionsInRange(650) == 0 && Q.GetSpellDamage(Enemy) > Enemy.TotalShieldHealth())
                    {
                        if (Q.CastMinimumHitchance(Enemy, 60)) { return; }
                    }

                    if (RInCombo.CurrentValue && flDistance < new[] { 2000, 2500, 3000 }[Hero.Spellbook.GetSpell(SpellSlot.R).Level - 1] && flDistance > UltRange.CurrentValue && Hero.Spellbook.GetSpell(SpellSlot.R).IsReady && Enemy.TotalShieldHealth() - Hero.GetSpellDamage(Enemy, SpellSlot.R) < 0 && Hero.CountEnemyChampionsInRange(UltRange.CurrentValue) == 0)
                    {
                        if (EnemyToBlockR.CurrentValue && Enemy.CountAllyChampionsInRange(500) > 0)
                            continue;
                        if (Player.CastSpell(SpellSlot.R, Enemy)) { return; }
                    }

                    if (TrapImmobileCombo.CurrentValue && flDistance < W.Range && (Enemy.HasBuffOfType(BuffType.Snare) || Enemy.HasBuffOfType(BuffType.Stun) || Enemy.HasBuffOfType(BuffType.Suppression) || Enemy.HasBuffOfType(BuffType.Knockup)))
                    {
                        if (Environment.TickCount - LastTrapTime > WDelay.CurrentValue)
                        {
                            if (W.Cast(Enemy))
                            {
                                LastTrapTime = Environment.TickCount;
                                return;
                            }
                        }
                    }

                    if (EWhenClose.CurrentValue && flDistance < 300)
                    {
                        if (E.Cast(Enemy))
                        {
                            ComboTarget = Enemy;
                            UseNetCombo = true;
                        }
                    }
                }
            }
        }

        private static bool ComboTrap { get; set; }
        private static bool UseNet { get; set; }
        private static bool UseNetCombo { get; set; }
        private static float LastTrapTime { get; set; }
        private static AIHeroClient ComboTarget { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += delegate
            {
                if (Player.Instance.Hero == Champion.Caitlyn)
                {
                    new Caitlyn();
                }
            };
        }
    }
}
