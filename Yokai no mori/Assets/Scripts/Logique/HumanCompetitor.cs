using UnityEngine;
using YokaiNoMori.Enumeration;
using YokaiNoMori.Interface;

namespace YokaiNoMori.BackEnd
{
    public class HumanCompetitor : ICompetitor
    {
        private IGameManager _gameManager;
        private float _timerForAI;
        private ECampType _camp;
        private string _name;

        public HumanCompetitor(string name)
        {
            _name = name;
        }

        public void Init(IGameManager igameManager, float timerForAI, ECampType currentCamp)
        {
            _gameManager = igameManager;
            _timerForAI = timerForAI;
            _camp = currentCamp;
        }

        public string GetName() => _name;
        public ECampType GetCamp() => _camp;

        public void GetDatas()
        {
        }

        public void StartTurn()
        {
            Debug.Log($"Turn started for {_name} ({_camp})");
        }

        public void StopTurn()
        {
            Debug.Log($"Turn stopped for {_name} ({_camp})");
        }
    }
}
