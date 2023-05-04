using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public List<Sprite> Sprites = new List<Sprite>();           //Dùng để lưu trữ image của các item
    public GameObject TilePrefab;
    public int GridDimension = 8;
    public float Distance = 1.0f;
    private GameObject[,] Grid;
    public static GridManager Instance { get; private set; } 
    public GameObject GameOverMenu;
    public TextMeshProUGUI MovesText;
    public TextMeshProUGUI ScoreText;
    public int StartingMoves = 50;
    private int _numMoves;
    public int NumMoves
    {
        get
        {
            return _numMoves;
        }
        set
        {
            _numMoves = value;
            MovesText.text = _numMoves.ToString();
        }
    }

    private int _score;
    public int Score
    {
        get
        {
            return _score;
        }
        set
        {
            _score = value;
            ScoreText.text = _score.ToString();
        }
    }
    void Awake() 
    {
        Instance = this;
        Score = 0;
        NumMoves = StartingMoves;
        GameOverMenu.SetActive(false);
    }
    private void Start() {
        Grid = new GameObject[GridDimension, GridDimension];
        InitGrid();
    }

    void InitGrid()
    {
        Vector3 positionOffset = transform.position - new Vector3(GridDimension * Distance / 2.0f, GridDimension * Distance / 2.0f, 0);
        for (int row = 0; row < GridDimension; row++)
        {
            for(int column = 0; column< GridDimension; column++)
            {
                List<Sprite> possibleSprites = Sprites.GetRange(0, Sprites.Count);; // 1
                //Choose what sprite to use for this cell
                Sprite left1 = GetSpriteAt(column - 1, row); //2
                Sprite left2 = GetSpriteAt(column - 2, row);
                if (left2 != null && left1 == left2) // 3
                {
                    possibleSprites.Remove(left1); // 4
                }

                Sprite down1 = GetSpriteAt(column, row - 1); // 5
                Sprite down2 = GetSpriteAt(column, row - 2);
                if (down2 != null && down1 == down2)
                {
                    possibleSprites.Remove(down1);
                }
                GameObject newTile = Instantiate(TilePrefab);
                SpriteRenderer renderer = newTile.GetComponent<SpriteRenderer>();
                renderer.sprite = possibleSprites[Random.Range(0, possibleSprites.Count)];
                Tile tile = newTile.AddComponent<Tile>();
                tile.Position = new Vector2Int(column, row);
                newTile.transform.parent = transform;
                newTile.transform.position = new Vector3(column * Distance, row * Distance, 0) + positionOffset;
                Grid[column,row] = newTile;
            }
            
        }
    }
    
    int check(Sprite currentSprite, int row, int column)
    {
        if(column>=2 && Grid[column-2, row]!=null) // kiểm tra null trước khi truy xuất sprite
        {
            if(Grid[column-2, row].GetComponent<SpriteRenderer>().sprite == Grid[column-1, row].GetComponent<SpriteRenderer>().sprite)
            {
                if(Grid[column-1, row].GetComponent<SpriteRenderer>().sprite == currentSprite)
                    return 0;
            }
        }
        if(row>=2 && Grid[column, row-2]!=null) // kiểm tra null trước khi truy xuất sprite
        {
            if(Grid[column, row-2].GetComponent<SpriteRenderer>().sprite == Grid[column, row-1].GetComponent<SpriteRenderer>().sprite)
            {
                if(Grid[column, row-1].GetComponent<SpriteRenderer>().sprite == currentSprite)
                    return 0;
            }
        }
        return 1;
    }
    

    public void SwapTiles(Vector2Int tile1Position, Vector2Int tile2Position)
    {
        GameObject tile1 = Grid[tile1Position.x, tile1Position.y];
        SpriteRenderer renderer1 = tile1.GetComponent<SpriteRenderer>();

        GameObject tile2 = Grid[tile2Position.x, tile2Position.y];
        SpriteRenderer renderer2 = tile2.GetComponent<SpriteRenderer>();

        Sprite temp = renderer1.sprite;
        renderer1.sprite = renderer2.sprite;
        renderer2.sprite = temp;

        bool changesOccurs = CheckMatches();
        if(!changesOccurs)
        {
            temp = renderer1.sprite;
            renderer1.sprite = renderer2.sprite;
            renderer2.sprite = temp;
            SoundManager.Instance.PlaySound(SoundType.TypeMove);
        }
        else
        {
            SoundManager.Instance.PlaySound(SoundType.TypePop);
            NumMoves--;
            do
            {
                FillHoles();
            } while (CheckMatches());
        }
        if (NumMoves <= 0) 
        {
            NumMoves = 0; GameOver();
        }

    }

    private void SwapSprite(SpriteRenderer renderer1, SpriteRenderer renderer2)
    {
        Sprite temp = renderer1.sprite;
        renderer1.sprite = renderer2.sprite;
        renderer2.sprite = temp;
    }

    Sprite GetSpriteAt(int column, int row)
    {
        if (column < 0 || column >= GridDimension
            || row < 0 || row >= GridDimension)
            return null;
        GameObject tile = Grid[column, row];
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        return renderer.sprite;
    }


    bool CheckMatches()
    {
        HashSet<SpriteRenderer> matchedTiles = new HashSet<SpriteRenderer>();        
        for (int row = 0; row < GridDimension;row++)
        {
            for (int column = 0; column < GridDimension; column++)
            {
                SpriteRenderer current = GetSpriteRendererAt(column, row);
                List<SpriteRenderer> horizontalMatches = new List<SpriteRenderer>(FindColumnMatchForTile(column, row, current.sprite));
                if(horizontalMatches.Count>=2)
                {
                    matchedTiles.UnionWith(horizontalMatches);
                    matchedTiles.Add(current);
                }
                List<SpriteRenderer> verticalMatches = new List<SpriteRenderer>(FindRowMatchForTile(column, row, current.sprite));
                if(verticalMatches.Count>=2)
                {
                    matchedTiles.UnionWith(verticalMatches);
                    matchedTiles.Add(current);
                }
            }
        }
        foreach (SpriteRenderer renderer in matchedTiles)
        {
            renderer.sprite = null;
        }
        Debug.Log(matchedTiles.Count);
        Score += matchedTiles.Count;
        return matchedTiles.Count>0;    
    }

    List<SpriteRenderer> FindRowMatchForTile(int column, int row, Sprite sprite)
    {
        List<SpriteRenderer> result = new List<SpriteRenderer>();
        for(int i = column + 1; i<GridDimension;i++)
        {
            SpriteRenderer nextColumn = GetSpriteRendererAt(i, row);
            if(nextColumn.sprite !=sprite)
            {
                break;
            }
            result.Add(nextColumn);
        }
        // for(int i = column - 1; i>=0; i--)
        // {
        //     SpriteRenderer nextColumn = GetSpriteRendererAt(i, row);
        //     if(nextColumn.sprite !=sprite)
        //     {
        //         break;
        //     }
        //     result.Add(nextColumn);
        // }
        return result;
    }

    List<SpriteRenderer> FindColumnMatchForTile(int column, int row, Sprite sprite)
    {
        List<SpriteRenderer> result = new List<SpriteRenderer>();
        for(int i = row + 1; i< GridDimension; i++)
        {
            SpriteRenderer nextRow = GetSpriteRendererAt(column, i);
            if(nextRow.sprite !=sprite)
            {
                break;
            }
            result.Add(nextRow);
        }
        // for(int i = row - 1; i>=0; i--)
        // {
        //     SpriteRenderer nextRow = GetSpriteRendererAt(column, i);
        //     if(nextRow.sprite != sprite)
        //     {
        //         break;
        //     }
        //     result.Add(nextRow);
        // }
        return result;
    }

    //Thay vì truy mỗi lần gọi hàm GetSpriteRendereAt rồi sử dụng hàm GetComponent thì ta tạo riêng một list chứa các dữ liệu từ sprite 
    //và hàm này mục đích truy cập vào vị trí của list đó
    SpriteRenderer GetSpriteRendererAt(int column, int row)
    {
        if (column < 0 || column >= GridDimension|| row < 0 || row >= GridDimension)
            return null;
        GameObject tile = Grid[column, row];
        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        return renderer;
    }

    void FillHoles()
    {
        for(int column = 0; column<GridDimension;column++)
        {
            for(int row = 0; row<GridDimension;row++)
            {
                while(GetSpriteRendererAt(column,row).sprite==null)
                {
                    for(int filler = row; filler<GridDimension-1;filler++)
                    {
                        SpriteRenderer current = GetSpriteRendererAt(column, filler);
                        SpriteRenderer next = GetSpriteRendererAt(column, filler + 1);
                        current.sprite = next.sprite;
                    }
                    SpriteRenderer last = GetSpriteRendererAt(column,GridDimension-1);
                    last.sprite = Sprites[Random.Range(0, Sprites.Count)];
                }
            }
        }
    }
    void GameOver()
    {
        PlayerPrefs.SetInt ("score", Score);
        GameOverMenu.SetActive (true);
    }
}
