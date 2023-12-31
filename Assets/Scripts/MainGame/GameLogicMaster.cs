using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sabanihsi.ScreenSystem;
using Sabanishi.MainGame.Stage;
using UniRx;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Sabanishi.MainGame
{
    /// <summary>
    /// ゲームロジックのトップモジュール
    /// </summary>
    public class GameLogicMaster:MonoBehaviour
    {
        [SerializeField] private PlayerPresenter _playerPresenterPrefab;
        [SerializeField] private StagePresenter _stagePresenter;
        [SerializeField] private MainCamera _mainCamera;
        [SerializeField]private StartEffectAnimation _startEffectAnimation;
        [SerializeField]private ResultPanel _resultPanel;
        
        private PlayerPresenter _player;
        private int _doorX;
        private Action _gotoStageSelectAction;
        private DateTime _startTime;
        private bool _isClear;
        
        private Dictionary<string, string> _stageTilemapPathDict = new()
        {
            { "Stage1", "Tilemap/Stage1" },
            { "Stage2", "Tilemap/Stage2" },
            { "Stage3", "Tilemap/Stage3" },
            { "Stage4", "Tilemap/Stage4" },
            {"StageSelect","Tilemap/StageSelect"}
        };

        /// <summary>
        /// Screen生成時に呼ばれる処理
        /// </summary>
        public void Initialize(StageData stageData,Action gotoStageSelectAction)
        {
            _gotoStageSelectAction = gotoStageSelectAction;
            _player = Instantiate(_playerPresenterPrefab,transform);

            if (TryGetStageTilemap(stageData.TileName, out var tilemap))
            {
                _stagePresenter.Initialize(tilemap);
            }
            
            _player.Initialize(_stagePresenter.PlayerRespawnPos);
            _player.AddBoxSubject.Subscribe(_stagePresenter.Model.AddBox).AddTo(gameObject);
            _player.RemoveBoxSubject.Subscribe(_stagePresenter.Model.RemoveBox).AddTo(gameObject);
            
            _mainCamera.Initialize(_player.transform,_stagePresenter.MapSize);

            _doorX = _stagePresenter.DoorPos.x;
            _startTime = DateTime.Now;
        }

        public void Open(bool isStageSelect)
        {
            _player.Model.SetCanOperate(true);
            if (!isStageSelect)
            {
                SoundManager.PlaySE(SE_Enum.HUE);
                _startEffectAnimation.StartEffect(_stagePresenter.PlayerRespawnPos);
            }
        }

        public void Dispose()
        {
            _player.Dispose();
            _stagePresenter.Dispose();
        }

        private bool TryGetStageTilemap(string stageName, out Tilemap tilemap)
        {
            tilemap = null;
            if (!_stageTilemapPathDict.TryGetValue(stageName, out var path))
            {
                Debug.LogError("[ScreenTransition] ScreenEnumに対応するパスが見つかりませんでした: " + stageName);
                return false;
            }

            tilemap = Resources.Load<Tilemap>(path);
            if (tilemap == null)
            {
                Debug.Log("[ScreenTransition] Tilemapの取得に失敗しました");
            }

            return true;
        }

        private void Update()
        {
            if (_isClear)
            {
                _player.Model.SetCanOperate(false);
                return;
            }
            
            if (Input.GetButtonDown("Reset"))
            {
                ScreenTransition.Instance.Move(ScreenEnum.MainGame).Forget();
            }

            if (Input.GetButtonDown("Quit"))
            {
                _gotoStageSelectAction?.Invoke();
                ScreenTransition.Instance.Move(ScreenEnum.MainGame).Forget();
            }
            
            _isClear = CheckIsClear();
            if (_isClear)
            {
                SoundManager.PlaySE(SE_Enum.CLEAR);
                _gotoStageSelectAction?.Invoke();
                var nowTime = DateTime.Now;
                _resultPanel.Show(nowTime-_startTime,_player.Model.PaintCount);
                SoundManager.StopBGM();
            }
        }

        /// <summary>
        /// クリアフラグをチェックする
        /// </summary>
        private bool CheckIsClear()
        {
            if (_player.Model.IsHang.Value) return false;

            return _stagePresenter.IsAllBoxIndoor(_doorX);
        }
    }
}