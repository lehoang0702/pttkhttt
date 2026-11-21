using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MedievalRPG.Data;

namespace MedievalRPG.Combat
{
    public class TurnBattleManager : MonoBehaviour
    {
        public static TurnBattleManager Instance { get; private set; }

        [Header("Party & Enemies")]
        [SerializeField] private List<BattleUnit> playerParty = new List<BattleUnit>();
        [SerializeField] private List<BattleUnit> enemyParty = new List<BattleUnit>();

        [Header("Turn State")]
        [SerializeField] private int currentRound = 1;
        private List<BattleUnit> _turnQueue = new List<BattleUnit>();
        private BattleUnit _activeUnit;
        private bool _isActionInProgress = false;

        public int CurrentRound => currentRound;
        public BattleUnit ActiveUnit => _activeUnit;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            StartCoroutine(StartBattleDelayed());
        }

        private IEnumerator StartBattleDelayed()
        {
            yield return new WaitForSeconds(0.2f);
            StartNewRound();
        }

        public void StartNewRound()
        {
            _turnQueue.Clear();

            // Collect alive units
            foreach (var u in playerParty) if (u != null && !u.IsDead) _turnQueue.Add(u);
            foreach (var e in enemyParty) if (e != null && !e.IsDead) _turnQueue.Add(e);

            // Sort turn queue by Speed descending (highest speed goes first)
            _turnQueue.Sort((a, b) =>
            {
                int speedA = a.ClassData != null ? a.ClassData.speed : 10;
                int speedB = b.ClassData != null ? b.ClassData.speed : 10;
                return speedB.CompareTo(speedA);
            });

            Debug.Log($"<color=yellow>=== ROUND {currentRound} STARTED ===</color>");
            foreach (var u in _turnQueue)
            {
                Debug.Log($"Turn Order: [{u.ClassData.className}] (Speed: {u.ClassData.speed})");
            }

            AdvanceTurn();
        }

        public void AdvanceTurn()
        {
            if (CheckBattleEnd()) return;

            if (_turnQueue.Count == 0)
            {
                currentRound++;
                StartNewRound();
                return;
            }

            _activeUnit = _turnQueue[0];
            _turnQueue.RemoveAt(0);

            if (_activeUnit.IsDead)
            {
                AdvanceTurn();
                return;
            }

            _activeUnit.OnTurnStart();

            // If it's an enemy, perform simple AI action automatically
            if (_activeUnit.IsEnemy)
            {
                StartCoroutine(ExecuteEnemyTurn(_activeUnit));
            }
        }

        private IEnumerator ExecuteEnemyTurn(BattleUnit enemy)
        {
            _isActionInProgress = true;
            yield return new WaitForSeconds(0.8f);

            // Pick a random alive player
            List<BattleUnit> alivePlayers = playerParty.FindAll(p => !p.IsDead);
            if (alivePlayers.Count > 0)
            {
                BattleUnit target = alivePlayers[Random.Range(0, alivePlayers.Count)];
                if (enemy.ClassData != null && enemy.ClassData.skills.Count > 0)
                {
                    SkillData skill = enemy.ClassData.skills[0];
                    enemy.ExecuteSkill(skill, new List<BattleUnit> { target }, () =>
                    {
                        _isActionInProgress = false;
                        AdvanceTurn();
                    });
                }
                else
                {
                    _isActionInProgress = false;
                    AdvanceTurn();
                }
            }
            else
            {
                _isActionInProgress = false;
                AdvanceTurn();
            }
        }

        public void PlayerAttack(SkillData skill, BattleUnit target)
        {
            if (_isActionInProgress || _activeUnit == null || _activeUnit.IsEnemy || _activeUnit.IsDead) return;

            _isActionInProgress = true;
            List<BattleUnit> targets = new List<BattleUnit>();

            if (skill.targetType == SkillTargetType.AllEnemies)
            {
                targets = enemyParty.FindAll(e => !e.IsDead);
            }
            else if (skill.targetType == SkillTargetType.SingleAlly)
            {
                targets.Add(target != null ? target : _activeUnit);
            }
            else if (skill.targetType == SkillTargetType.Self)
            {
                targets.Add(_activeUnit);
            }
            else
            {
                // Single Enemy
                if (target == null || target.IsDead)
                {
                    target = enemyParty.Find(e => !e.IsDead);
                }
                if (target != null) targets.Add(target);
            }

            _activeUnit.ExecuteSkill(skill, targets, () =>
            {
                _isActionInProgress = false;
                AdvanceTurn();
            });
        }

        public void PlayerDefend()
        {
            if (_isActionInProgress || _activeUnit == null || _activeUnit.IsEnemy || _activeUnit.IsDead) return;

            _activeUnit.Defend();
            AdvanceTurn();
        }

        private bool CheckBattleEnd()
        {
            bool allPlayersDead = playerParty.TrueForAll(p => p.IsDead);
            bool allEnemiesDead = enemyParty.TrueForAll(e => e.IsDead);

            if (allPlayersDead)
            {
                Debug.Log("<color=red>DEFEAT! All party members have fallen.</color>");
                return true;
            }

            if (allEnemiesDead)
            {
                Debug.Log("<color=green>VICTORY! All enemies have been vanquished!</color>");
                return true;
            }

            return false;
        }

        // --- Interactive Debug UI for quick testing ---
        private void OnGUI()
        {
            GUI.skin.box.fontSize = 14;
            GUI.skin.button.fontSize = 13;

            GUILayout.BeginArea(new Rect(15, 15, 360, 480), GUI.skin.box);
            GUILayout.Label($"<b>MEDIEVAL TURN-BASED RPG</b> (Round {currentRound})", GUI.skin.label);
            GUILayout.Space(5);

            if (_activeUnit != null && !_activeUnit.IsEnemy)
            {
                GUILayout.Label($"Current Turn: <color=yellow><b>{_activeUnit.ClassData.className}</b></color>");
                GUILayout.Label($"HP: {_activeUnit.CurrentHealth}/{_activeUnit.MaxHealth} | MP: {_activeUnit.CurrentMana}/{_activeUnit.MaxMana}");
                GUILayout.Space(5);

                if (!_isActionInProgress)
                {
                    GUILayout.Label("<b>Skills:</b>");
                    if (_activeUnit.ClassData != null)
                    {
                        foreach (var skill in _activeUnit.ClassData.skills)
                        {
                            string btnText = $"{skill.skillName} (MP: {skill.manaCost})";
                            bool canCast = _activeUnit.CurrentMana >= skill.manaCost;

                            GUI.enabled = canCast;
                            if (GUILayout.Button(btnText, GUILayout.Height(30)))
                            {
                                PlayerAttack(skill, null);
                            }
                            GUI.enabled = true;
                        }
                    }

                    if (GUILayout.Button("Defend (50% Damage Reduction)", GUILayout.Height(28)))
                    {
                        PlayerDefend();
                    }
                }
                else
                {
                    GUILayout.Label("<i>Action in progress...</i>");
                }
            }
            else if (_activeUnit != null)
            {
                GUILayout.Label($"Current Turn: <color=red><b>{_activeUnit.ClassData.className}</b></color> (Enemy)");
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>Party Status:</b>");
            foreach (var p in playerParty)
            {
                if (p == null) continue;
                string status = p.IsDead ? "<color=red>[Dead]</color>" : $"HP: {p.CurrentHealth}/{p.MaxHealth} MP: {p.CurrentMana}/{p.MaxMana}";
                GUILayout.Label($"{p.ClassData.className}: {status}");
            }

            GUILayout.Space(5);
            GUILayout.Label("<b>Enemies:</b>");
            foreach (var e in enemyParty)
            {
                if (e == null) continue;
                string status = e.IsDead ? "<color=red>[Dead]</color>" : $"HP: {e.CurrentHealth}/{e.MaxHealth}";
                GUILayout.Label($"{e.ClassData.className}: {status}");
            }

            GUILayout.EndArea();
        }
    }
}
