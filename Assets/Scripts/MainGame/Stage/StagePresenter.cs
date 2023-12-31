using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Sabanishi.MainGame.Stage
{
    public class StagePresenter:MonoBehaviour
    {
        [SerializeField] private StageView _view;
        private StageModel _model;
        public StageModel Model => _model;
        
        private Vector3 _playerRespawnPos;
        public Vector3 PlayerRespawnPos => _playerRespawnPos;
        private Vector3Int _doorPos;
        public Vector3Int DoorPos => _doorPos;
        private Vector2 _mapSize;
        public Vector2 MapSize => _mapSize;

        // ReSharper disable Unity.PerformanceAnalysis
        public void Initialize(Tilemap tilemap)
        {
            var chipData = ConvertToChipEnumArray(tilemap);
            _model = new StageModel();
            _mapSize=new Vector2(chipData.GetLength(0),chipData.GetLength(1));
            _view.Initialize(chipData.GetLength(0), chipData.GetLength(1));

            //modelとの紐付け
            _model.StageData.ObserveAdd().Subscribe(_view.OnStageChipAdded).AddTo(gameObject);
            _model.StageData.ObserveReplace().Subscribe(_view.OnStageChipReplaced).AddTo(gameObject);
            _model.StageData.ObserveRemove().Subscribe(_view.OnStageChipRemoved).AddTo(gameObject);
            _model.DropBoxSubject.Subscribe(_view.DropBox).AddTo(gameObject);
            _model.GetChipObject = _view.GetBlock;
            
            _model.CreateBlock(chipData);
        }
        
        /// <summary>
        /// TilemapをChipEnum[,]に変換する
        /// </summary>
        /// <param name="tilemap"></param>
        /// <returns></returns>
        private ChipData[,] ConvertToChipEnumArray(Tilemap tilemap)
        {
            tilemap.CompressBounds();
            var bounds = tilemap.cellBounds;
            ChipData[,] chipData = new ChipData[bounds.size.x, bounds.size.y];
            TileBase[] allBlocks = tilemap.GetTilesBlock(bounds);
            for(int x=0;x<bounds.size.x;x++)
            {
                for(int y=0;y<bounds.size.y;y++)
                {
                    var tile = allBlocks[x + y * bounds.size.x];
                    var chipEnum = ChipEnum.None;
                    if(tile is not null)
                    {
                        if (tile.name.Contains("Floor2"))
                        {
                            chipEnum = ChipEnum.CannotPaintBlock;
                        }else if (tile.name.Contains("Floor"))
                        {
                            chipEnum= ChipEnum.CanPaintBlock;   
                        }else if (tile.name.Equals("Start"))
                        {
                            _playerRespawnPos = new Vector3(x, y, 0);
                        }else if (tile.name.Equals("Box"))
                        {
                            chipEnum = ChipEnum.Box;
                        }else if (tile.name.Equals("Door"))
                        {
                            _doorPos = new Vector3Int(x, y, 0);
                        }else if (tile.name.Equals("Ground"))
                        {
                            chipEnum = ChipEnum.CannotPaintBlock;
                        }else if (tile.name.Equals("BackTile"))
                        {
                            chipEnum = ChipEnum.IndoorBack;
                        }else if (int.TryParse(tile.name, out var stageNum))
                        {
                            chipEnum = ChipEnum.Flag;
                            Debug.Log(stageNum);
                            chipData[x, y] = new ChipData(ChipEnum.Flag,((Tile)tile)?.sprite, x, y, stageNum);
                        }else if (tile.name.Contains("Tutorial"))
                        {
                            chipEnum = ChipEnum.Tutorial;
                        }
                    }
                    
                    if (!chipEnum.Equals(ChipEnum.None)&&!chipEnum.Equals(ChipEnum.Flag))
                    {
                        //tileの画像を取得する
                        var sprite = ((Tile)tile)?.sprite;
                        chipData[x, y] = new ChipData(chipEnum, sprite,x,y);
                    }
                }
            }
            return chipData;
        }

        public void Dispose()
        {
            _model.Dispose();
        }

        /// <summary>
        /// 全ての箱が室内に存在すればtrueを返す
        /// </summary>
        public bool IsAllBoxIndoor(int x)
        {
            return _model.IsAllBlockIndoor(x);
        }
    }
}