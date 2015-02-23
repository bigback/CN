using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.IO;
using SharpDX;
using Collision = LeagueSharp.Common.Collision;
namespace Jinx
{
    class Program
    {
        public const string ChampionName = "Jinx";

        //Orbwalker instance
        public static Orbwalking.Orbwalker Orbwalker;
        
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell R1;
        //ManaMenager
        public static float QMANA;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;
        public static bool Farm = false;
        public static bool attackNow = true;
        public static double WCastTime = 0;
        public static double QCastTime = 0;
        //AutoPotion
        public static Items.Item Potion = new Items.Item(2003, 0);
        public static Items.Item ManaPotion = new Items.Item(2004, 0);
        public static Items.Item Youmuu = new Items.Item(3142, 0);

        //Menu
        public static Menu Config;

        private static Obj_AI_Hero Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;
            if (Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, float.MaxValue);
            W = new Spell(SpellSlot.W, 1500f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 2500f);
            R1 = new Spell(SpellSlot.R, 2500f);

            W.SetSkillshot(0.6f, 60f, 3300f, true, SkillshotType.SkillshotLine);
            E.SetSkillshot(1.2f, 1f, 1750f, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.7f, 140f, 1500f, false, SkillshotType.SkillshotLine);
            R1.SetSkillshot(0.7f, 200f, 1500f, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
            SpellList.Add(R1);
            Config = new Menu(ChampionName, ChampionName, true);
            Config = new Menu("OneKeyToWin_金克丝", "name", true);
            var targetSelectorMenu = new Menu("目标选择器", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);
            Config.AddSubMenu(new Menu("走砍", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));
			Config.AddSubMenu(new Menu("后爸爸汉化", "hanhua"));
            Config.AddToMainMenu();
            #region E
                Config.SubMenu("E 设置").AddItem(new MenuItem("autoE", "连招自动E(测试)").SetValue(true));
                Config.SubMenu("E 设置").AddItem(new MenuItem("AGC", "使用E反突进").SetValue(true));
                Config.SubMenu("E 设置").AddItem(new MenuItem("opsE", "施法过程中使用E").SetValue(true));
            #endregion
            #region R
                Config.SubMenu("R 设置").AddItem(new MenuItem("autoR", "自动 R").SetValue(true));
                Config.SubMenu("R 设置").AddItem(new MenuItem("hitchanceR", "R 高命中率").SetValue(true));
                Config.SubMenu("R 设置").AddItem(new MenuItem("useR", "半手动使用R按键").SetValue(new KeyBind('t', KeyBindType.Press))); //32 == space
            #endregion
            Config.AddItem(new MenuItem("noti", "显示提示").SetValue(true));
            Config.AddItem(new MenuItem("pots", "使用 pots").SetValue(true));
            Config.AddItem(new MenuItem("Hit", "W 命中率").SetValue(new Slider(2, 2, 0)));
            Config.AddItem(new MenuItem("debug", "调试").SetValue(false));

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameUpdate += Game_OnGameUpdate;
            Orbwalking.BeforeAttack += BeforeAttack;
            Orbwalking.AfterAttack += afterAttack;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Game.PrintChat("<font color=\"#ff00d8\">J</font>inx full automatic AI ver 2.5.1 <font color=\"#000000\">by sebastiank1</font> - <font color=\"#00BFFF\">鍔犺浇鎴愬姛</font>");
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            ManaMenager();
            if (Orbwalker.ActiveMode.ToString() == "Mixed" || Orbwalker.ActiveMode.ToString() == "LaneClear" || Orbwalker.ActiveMode.ToString() == "LastHit")
                Farm = true;
            else
                Farm = false;

            if (E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA)
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(E.Range) && E.IsReady()))
                {
                    if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                         enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                         enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Suppression) ||
                         enemy.IsStunned || enemy.HasBuff("Recall"))
                        E.Cast(enemy, true);
                    else if (enemy.HasBuffOfType(BuffType.Slow) && enemy.Path.Count() > 1)
                        E.CastIfHitchanceEquals(enemy, HitChance.VeryHigh, true);
                    else if (enemy.Path.Count() > 1 && enemy.CountEnemiesInRange(300) > 2)
                        E.CastIfHitchanceEquals(enemy, HitChance.VeryHigh, true);
                    else
                        E.CastIfHitchanceEquals(enemy, HitChance.Immobile, true);
                }
                foreach (var Object in ObjectManager.Get<Obj_AI_Base>().Where(Obj => Obj.Distance(Player.ServerPosition) < E.Range && E.IsReady() && Obj.Team != Player.Team && (Obj.HasBuff("teleport_target", true) || Obj.HasBuff("Pantheon_GrandSkyfall_Jump", true))))
                {
                    E.Cast(Object.Position, true);
                }
                var ta = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                if (Orbwalker.ActiveMode.ToString() == "Combo" && E.IsReady() && ta.IsValidTarget(E.Range) && Config.Item("autoE").GetValue<bool>() && ObjectManager.Player.Mana > RMANA + EMANA + WMANA && ta.Path.Count() == 1)
                {
                    if (ObjectManager.Player.Position.Distance(ta.ServerPosition) > ObjectManager.Player.Position.Distance(ta.Position))
                    {
                        if (ta.Position.Distance(ObjectManager.Player.ServerPosition) < ta.Position.Distance(ObjectManager.Player.Position) && ta.IsValidTarget(E.Range))
                        {
                            E.CastIfHitchanceEquals(ta, HitChance.VeryHigh, true);
                            debug("E run");
                        }
                    }
                    else
                    {
                        if (ta.Position.Distance(ObjectManager.Player.ServerPosition) > ta.Position.Distance(ObjectManager.Player.Position) && ta.IsValidTarget(E.Range))
                        {
                            E.CastIfHitchanceEquals(ta, HitChance.VeryHigh, true);
                            debug("E escape");
                        }
                    }
                }
            }

            if (Q.IsReady())
            {
                ManaMenager();
                if (Farm)
                    if (ObjectManager.Player.Mana > RMANA + WMANA + EMANA + 10 && !FishBoneActive)
                        farmQ();
                var t = TargetSelector.GetTarget(bonusRange() + 50, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget())
                {
                    var distance = GetRealDistance(t);
                    var powPowRange = GetRealPowPowRange(t);
                    if (!FishBoneActive && !Orbwalking.InAutoAttackRange(t))
                    {
                        if (Orbwalker.ActiveMode.ToString() == "Combo" && (ObjectManager.Player.Mana > RMANA + WMANA + 20 || ObjectManager.Player.GetAutoAttackDamage(t) * 2 > t.Health))
                            Q.Cast();
                        else if (Orbwalker.ActiveMode.ToString() == "Mixed" && haras() && !ObjectManager.Player.UnderTurret(true) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + WMANA + 20 && distance < bonusRange() + t.BoundingRadius + ObjectManager.Player.BoundingRadius)
                            Q.Cast();
                        else if (Orbwalker.ActiveMode.ToString() == "LaneClear" && haras() && !ObjectManager.Player.UnderTurret(true) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + WMANA + 20 && distance < bonusRange())
                            Q.Cast();
                    }
                    else if (FishBoneActive && attackNow)
                    {
                        if (Orbwalker.ActiveMode.ToString() == "Combo" && (distance < powPowRange) && (ObjectManager.Player.Mana < RMANA + WMANA + 20 || ObjectManager.Player.GetAutoAttackDamage(t) * 2 < t.Health))
                            Q.Cast();
                        else if (Farm && (distance > bonusRange() || distance < powPowRange || ObjectManager.Player.Mana < RMANA + EMANA + WMANA + WMANA))
                            Q.Cast();
                    }
                }
                else if (!FishBoneActive && (Orbwalker.ActiveMode.ToString() == "Combo") && ObjectManager.Player.Mana > RMANA + WMANA + 20)
                    Q.Cast();
                else if (FishBoneActive && (Orbwalker.ActiveMode.ToString() == "Combo") && ObjectManager.Player.Mana < RMANA + WMANA + 20)
                    Q.Cast();
                else if (FishBoneActive && Farm)
                    Q.Cast();
            }

            if (W.IsReady() && (Game.Time - QCastTime > 0.6))
            {
                ManaMenager();
                bool cast = false;
                bool wait = false;
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (target.IsValidTarget(W.Range) &&
                        !target.HasBuffOfType(BuffType.PhysicalImmunity) && !target.HasBuffOfType(BuffType.SpellImmunity) && !target.HasBuffOfType(BuffType.SpellShield))
                    {
                        float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(W.Delay + (Player.Distance(target.ServerPosition) / W.Speed) * 1000));
                        var Wdmg = W.GetDamage(target);
                        if (Wdmg > predictedHealth)
                        {
                            cast = true;
                            wait = true;
                            PredictionOutput output = R.GetPrediction(target);
                            Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
                            direction.Normalize();
                            List<Obj_AI_Hero> enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
                            foreach (var enemy in enemies)
                            {
                                if (enemy.SkinName == target.SkinName || !cast)
                                    continue;
                                PredictionOutput prediction = R.GetPrediction(enemy);
                                Vector3 predictedPosition = prediction.CastPosition;
                                Vector3 v = output.CastPosition - Player.ServerPosition;
                                Vector3 w = predictedPosition - Player.ServerPosition;
                                double c1 = Vector3.Dot(w, v);
                                double c2 = Vector3.Dot(v, v);
                                double b = c1 / c2;
                                Vector3 pb = Player.ServerPosition + ((float)b * v);
                                float length = Vector3.Distance(predictedPosition, pb);
                                if (length < (W.Width + enemy.BoundingRadius) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                    cast = false;
                            }
                            if (!Orbwalking.InAutoAttackRange(target) && cast && target.IsValidTarget(W.Range) && ObjectManager.Player.CountEnemiesInRange(400) == 0)
                            {
                                W.Cast(target, true);
                                debug("W ks");
                            }
                        }
                    }
                }

                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (t.IsValidTarget() && W.IsReady() && !wait)
                {
                    if (Orbwalker.ActiveMode.ToString() == "Combo" && ObjectManager.Player.Mana > RMANA + WMANA + 10 && ObjectManager.Player.CountEnemiesInRange(GetRealPowPowRange(t)) == 0)
                    {
                        castW(t);
                    }
                    else if ((Farm && ObjectManager.Player.Mana > RMANA + EMANA + WMANA + WMANA + 20) && !ObjectManager.Player.UnderTurret(true) && ObjectManager.Player.CountEnemiesInRange(bonusRange()) == 0 && haras())
                    {
                        castW(t);
                    }
                    else if ((Orbwalker.ActiveMode.ToString() == "Combo" || Farm) && ObjectManager.Player.Mana > RMANA + WMANA && ObjectManager.Player.CountEnemiesInRange(GetRealPowPowRange(t)) == 0)
                    {
                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsValidTarget(W.Range)))
                        {
                            if (enemy.HasBuffOfType(BuffType.Stun) || enemy.HasBuffOfType(BuffType.Snare) ||
                             enemy.HasBuffOfType(BuffType.Charm) || enemy.HasBuffOfType(BuffType.Fear) ||
                             enemy.HasBuffOfType(BuffType.Taunt) || enemy.HasBuffOfType(BuffType.Slow) || enemy.HasBuff("Recall"))
                            {
                                W.Cast(enemy, true);
                            }
                        }
                    }
                }
            }
            
            if (R.IsReady() && Config.Item("autoR").GetValue<bool>())
            {
                bool cast = false;
                foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
                {
                    if (target.IsValidTarget() && (Game.Time - WCastTime > 1) &&
                        !target.HasBuffOfType(BuffType.PhysicalImmunity) && !target.HasBuffOfType(BuffType.SpellImmunity) && !target.HasBuffOfType(BuffType.SpellShield))
                    {
                        float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(R.Delay + (Player.Distance(target.ServerPosition) / R.Speed) * 1000));
                        var Rdmg = R.GetDamage(target);
                        if (Rdmg > predictedHealth)
                        {
                            cast = true;
                            PredictionOutput output = R.GetPrediction(target);
                            Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
                            direction.Normalize();
                            List<Obj_AI_Hero> enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
                            foreach (var enemy in enemies)
                            {
                                if (enemy.SkinName == target.SkinName || !cast)
                                    continue;
                                PredictionOutput prediction = R.GetPrediction(enemy);
                                Vector3 predictedPosition = prediction.CastPosition;
                                Vector3 v = output.CastPosition - Player.ServerPosition;
                                Vector3 w = predictedPosition - Player.ServerPosition;
                                double c1 = Vector3.Dot(w, v);
                                double c2 = Vector3.Dot(v, v);
                                double b = c1 / c2;
                                Vector3 pb = Player.ServerPosition + ((float)b * v);
                                float length = Vector3.Distance(predictedPosition, pb);
                                if (length < (R.Width + 150 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                    cast = false;
                            }
                            
                            if (cast && target.IsValidTarget(R.Range) && GetRealDistance(target) > bonusRange() + 200 + target.BoundingRadius && target.CountAlliesInRange(600) == 0 && ObjectManager.Player.CountEnemiesInRange(400) == 0)
                            {
                                if (Config.Item("hitchanceR").GetValue<bool>() && target.Path.Count() < 2)
                                {
                                    R.CastIfHitchanceEquals(target, HitChance.VeryHigh, true);
                                    debug("R normal");
                                }
                                else
                                {
                                    R.Cast(target, true);
                                    debug("R normal");
                                }
                            }
                            else if (cast && target.IsValidTarget(R.Range) && target.CountEnemiesInRange(200) > 2 && GetRealDistance(target) > bonusRange() + 200 + target.BoundingRadius )
                            {
                                R1.Cast(target, true, true);
                                debug("R aoe 1");
                            }
                            else if (cast && target.HasBuff("Recall"))
                            {
                                R.Cast(target, true, true);
                                debug("R recall");
                            }
                        }
                    }
                }
            }
            PotionMenager();
        }

        private static void afterAttack(AttackableUnit unit, AttackableUnit target)
        {
            attackNow = true;
        }

        static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            attackNow = false;
            var t = TargetSelector.GetTarget(bonusRange() + 50, TargetSelector.DamageType.Physical);

            if (Q.IsReady() && Orbwalker.ActiveMode.ToString() == "LaneClear" && !FishBoneActive && ObjectManager.Player.Mana < RMANA + EMANA + WMANA + 30)
            {
                var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, bonusRange() + 30, MinionTypes.All);
                foreach (var minion in allMinionsQ)
                {
                    if (Orbwalking.InAutoAttackRange(minion) && minion.Health < ObjectManager.Player.GetAutoAttackDamage(minion))
                    {
                        foreach (var minion2 in allMinionsQ)
                        {
                            if (minion2.Health < ObjectManager.Player.GetAutoAttackDamage(minion2) && minion.ServerPosition.Distance(minion2.Position) < 150 && minion2.Position != minion.Position)
                            {
                                Q.Cast();
                            }
                        }
                    }
                }
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (Config.Item("AGC").GetValue<bool>() && E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA)
            {
                var Target = (Obj_AI_Hero)gapcloser.Sender;
                if (Target.IsValidTarget(E.Range))
                {
                    E.Cast(ObjectManager.Player.ServerPosition, true);
                    debug("E agc");
                }
                return;
            }
            return;
        }

        public static void farmQ()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, bonusRange() + 30, MinionTypes.All);
            foreach (var minion in allMinionsQ)
            {
                if (!Orbwalking.InAutoAttackRange(minion) && minion.Health < ObjectManager.Player.GetAutoAttackDamage(minion) && GetRealPowPowRange(minion) < GetRealDistance(minion) && bonusRange() < GetRealDistance(minion))
                {
                    Q.Cast();
                    return;
                }
            }
        }

        public static bool haras()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, bonusRange(), MinionTypes.All);
            var haras = true;
            foreach (var minion in allMinionsQ)
            {
                if (minion.Health < ObjectManager.Player.GetAutoAttackDamage(minion) * 1.5 && bonusRange() > GetRealDistance(minion))
                    haras = false;
            }
            if (haras)
                return true;
            else
                return false;
        }

        private static void castW(Obj_AI_Hero target)
        {
            if (Config.Item("Hit").GetValue<Slider>().Value == 0)
                W.Cast(target, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 1)
                W.CastIfHitchanceEquals(target, HitChance.High, true);
            else if (Config.Item("Hit").GetValue<Slider>().Value == 2 && target.Path.Count() < 2)
                W.CastIfHitchanceEquals(target, HitChance.High, true);
        }

        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            double ShouldUse = ShouldUseE(args.SData.Name);

            if (Config.Item("opsE").GetValue<bool>() && unit.Team != ObjectManager.Player.Team && ShouldUse >= 0f && unit.IsValidTarget(E.Range))
            {
                E.Cast(unit.ServerPosition, true);
                debug("E ope");
            }
            if (unit.IsMe && args.SData.Name == "JinxW")
            {
                WCastTime = Game.Time;
            }

            foreach (var target in ObjectManager.Get<Obj_AI_Hero>())
             {
                 if (args.Target.NetworkId == target.NetworkId && args.Target.IsEnemy)
                 {

                     var dmg = unit.GetSpellDamage(target, args.SData.Name);
                     double HpLeft = target.Health - dmg;
                     if ( HpLeft < 0 && target.IsValidTarget())
                     {
                         QCastTime = Game.Time;
                     }
                     if (!Orbwalking.InAutoAttackRange(target) && target.IsValidTarget(W.Range) && W.IsReady())
                     {
                         var wDmg = W.GetDamage(target);
                         if ( wDmg > HpLeft && HpLeft > 0)
                         {
                             W.Cast(target, true);
                             WCastTime = Game.Time;
                             debug("W ks OPS");
                         }
                     }
                     if (GetRealDistance(target) > bonusRange() + 200 + target.BoundingRadius &&  target.IsValidTarget(R.Range) && R.IsReady() && ObjectManager.Player.CountEnemiesInRange(400) == 0)
                     {
                         var rDmg = R.GetDamage(target);
                         var cast = true;
                         
                         PredictionOutput output = R.GetPrediction(target);
                         Vector2 direction = output.CastPosition.To2D() - Player.Position.To2D();
                         direction.Normalize();
                         List<Obj_AI_Hero> enemies = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && x.IsValidTarget()).ToList();
                         foreach (var enemy in enemies)
                         {
                             if (enemy.SkinName == target.SkinName || !cast)
                                 continue;
                             PredictionOutput prediction = R.GetPrediction(enemy);
                             Vector3 predictedPosition = prediction.CastPosition;
                             Vector3 v = output.CastPosition - Player.ServerPosition;
                             Vector3 w = predictedPosition - Player.ServerPosition;
                             double c1 = Vector3.Dot(w, v);
                             double c2 = Vector3.Dot(v, v);
                             double b = c1 / c2;
                             Vector3 pb = Player.ServerPosition + ((float)b * v);
                             float length = Vector3.Distance(predictedPosition, pb);
                             if (length < (R.Width + 100 + enemy.BoundingRadius / 2) && Player.Distance(predictedPosition) < Player.Distance(target.ServerPosition))
                                 cast = false;
                         }
                         if (rDmg > HpLeft && HpLeft > 0 && cast && target.CountAlliesInRange(500) == 0)
                         {
                             R.Cast(target, true);
                             debug("R OPS");
                         }
                     }
                     
                 }
            }
        }

        public static double ShouldUseE(string SpellName)
        {
            if (SpellName == "ThreshQ")
                return 0;
            if (SpellName == "KatarinaR")
                return 0;
            if (SpellName == "AlZaharNetherGrasp")
                return 0;
            if (SpellName == "GalioIdolOfDurand")
                return 0;
            if (SpellName == "LuxMaliceCannon")
                return 0;
            if (SpellName == "MissFortuneBulletTime")
                return 0;
            if (SpellName == "RocketGrabMissile")
                return 0;
            if (SpellName == "CaitlynPiltoverPeacemaker")
                return 0;
            if (SpellName == "EzrealTrueshotBarrage")
                return 0;
            if (SpellName == "InfiniteDuress")
                return 0;
            if (SpellName == "VelkozR")
                return 0;
            return -1;
        }

        public static float bonusRange()
        {
            return 620f + ObjectManager.Player.BoundingRadius + 50 + 25 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level;
        }

        private static bool FishBoneActive
        {
            get { return Math.Abs(ObjectManager.Player.AttackRange - 525f) > float.Epsilon; }
        }

        private static float GetRealPowPowRange(GameObject target)
        {
            return 610f + ObjectManager.Player.BoundingRadius + target.BoundingRadius;
        }

        private static float GetRealDistance(GameObject target)
        {
            return ObjectManager.Player.ServerPosition.Distance(target.Position) + ObjectManager.Player.BoundingRadius +
                   target.BoundingRadius;
        }

        public static void ManaMenager()
        {
            QMANA = 10;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            if (!R.IsReady())
                RMANA = WMANA - ObjectManager.Player.Level * 2;
            else
                RMANA = R.Instance.ManaCost; ;

            if (Farm)
                RMANA = RMANA + ObjectManager.Player.CountEnemiesInRange(2500) * 20;

            if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.2)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
            }
        }

        public static void PotionMenager()
        {
            if (Config.Item("pots").GetValue<bool>() && !ObjectManager.Player.InFountain() && !ObjectManager.Player.HasBuff("Recall"))
            {
                if (Potion.IsReady() && !ObjectManager.Player.HasBuff("RegenerationPotion", true))
                {
                    if (ObjectManager.Player.CountEnemiesInRange(700) > 0 && ObjectManager.Player.Health + 200 < ObjectManager.Player.MaxHealth)
                        Potion.Cast();
                    else if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.6)
                        Potion.Cast();
                }
                if (ManaPotion.IsReady() && !ObjectManager.Player.HasBuff("FlaskOfCrystalWater", true))
                {
                    if (ObjectManager.Player.CountEnemiesInRange(1200) > 0 && ObjectManager.Player.Mana < RMANA + WMANA + EMANA + 20)
                        ManaPotion.Cast();
                }
            }
        }

        public static void debug(string msg)
        {
            if (Config.Item("debug").GetValue<bool>())
                Game.PrintChat(msg);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            
            if (Config.Item("noti").GetValue<bool>())
            {
                var orbT=Orbwalker.GetTarget();
                if (orbT.IsValidTarget())
                    Render.Circle.DrawCircle(orbT.Position, 100, System.Drawing.Color.Aqua);
                var t = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Physical);
                float predictedHealth = HealthPrediction.GetHealthPrediction(t, (int)(R.Delay + (Player.Distance(t.ServerPosition) / R.Speed) * 1000));
                if (t.IsValidTarget() && R.IsReady())
                {
                    var rDamage = R.GetDamage(t);
                    if (rDamage > predictedHealth)
                    {
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Ult can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                        Render.Circle.DrawCircle(t.ServerPosition, 200, System.Drawing.Color.Red);
                    }
                    if (Config.Item("useR").GetValue<KeyBind>().Active)
                    {
                        R1.Cast(t, true, true);
                    }
                }
                var tw = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
                if (tw.IsValidTarget())
                {
                    var wDmg = W.GetDamage(tw);
                    if (wDmg > tw.Health)
                    {
                        Render.Circle.DrawCircle(ObjectManager.Player.ServerPosition, W.Range, System.Drawing.Color.Red);
                        Render.Circle.DrawCircle(tw.ServerPosition, 200, System.Drawing.Color.Red);
                        Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.4f, System.Drawing.Color.Red, "W can kill: " + t.ChampionName + " have: " + t.Health + "hp");
                    }
                    
                }
            }
            if (Config.Item("debug").GetValue<bool>())
            {
                Drawing.DrawText(Drawing.Width * 0.1f, Drawing.Height * 0.5f, System.Drawing.Color.Red, "Q can change: " + attackNow); 
            }
        }
    }
}