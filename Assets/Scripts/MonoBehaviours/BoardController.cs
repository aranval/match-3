using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardController : MonoBehaviour
{
    public BoardSO board;
    public GameObject tilePrefab;
    public bool movementConstraint = true;
    private GameObject[,] tiles;
    private Transform tmpTransform;
    private GameObject tmpTile;
    private BallSO ball;
    private string latestMatchFound;
    Vector3 firstTilePosition = new Vector3(-2, -1, 0);
    private int matchCount;
    private int loopCount = 0;
    

    // Start is called before the first frame update
    void Start()
    {
        CreateAndPopulateBoard(board);
    }

    // Update is called once per frame
    void Update()
    {
        SwapTiles();
    }

    /*
     * @board BoardSO
     * CreateAndPopulateBoard generate a board and fills it with
     * random balls.
     */
    private void CreateAndPopulateBoard(BoardSO board)
    {
        Debug.Log("CreateAndPoulateBoard called.");
        tiles = new GameObject[board.width, board.height];
        for (int x = 0; x < board.width; x++)
        {
            for (int y = 0; y < board.height; y++)
            {
                Vector3 tilePos = new Vector3(firstTilePosition.x + x, firstTilePosition.y + y, 0);
                int randomBall = Random.Range(0, board.ballTypes.Length);

                GameObject tile = Instantiate(tilePrefab, tilePos, Quaternion.identity) as GameObject;
                tiles[x, y] = tile;
                tile.transform.parent = this.transform;
                tile.GetComponent<Tile>().ballSO = board.ballTypes[randomBall];
                tile.GetComponent<Tile>().column = x;
                tile.GetComponent<Tile>().row = y;
                tile.GetComponent<SpriteRenderer>().sprite = tile.GetComponent<Tile>().ballSO.sprite;
                tile.name = x + "" + y + "Tile";
            }

        }
        FindAllMatches();
        while(matchCount>0) { CleanInitBoard(board); }
        Debug.Log("Loop count: " + loopCount);
    }

    /*
     *  @board BoardSO
     *  CleanInitBoard cleans auto-generated board of any matches
     *  Changes tiles that are in matches to random colours 
     */
    private void CleanInitBoard(BoardSO board)
    {
        loopCount++;
        Debug.Log("Cleaning initial board test. " + matchCount);
        foreach (GameObject tile in tiles)
        {
            if (tile.GetComponent<Tile>().dirty)
            {
                string prevBall = tile.GetComponent<Tile>().ballSO.ballName;
                while (prevBall == tile.GetComponent<Tile>().ballSO.ballName)
                {
                    int randomBall = Random.Range(0, board.ballTypes.Length);
                    tile.GetComponent<Tile>().ballSO = board.ballTypes[randomBall];
                    tile.GetComponent<SpriteRenderer>().sprite = tile.GetComponent<Tile>().ballSO.sprite;
                }
                tile.GetComponent<Tile>().dirty = false;
            }
        }
        FindAllMatches();
    }

    /*
    * FallDownAndRefillBoard iterates twice over the tiles. On the first run through
    * it identifies columns that need to drop and on the second run through
    * for the spots that were left dirty by the drop it picks a random colored balls 
    * and fills them.
    * Execution of bonus Tile matches at its current state does not work as intended - current
    * code is in place just to check if it works at least for some cases that are covered
    */
    private void FallDownAndRefillBoard()
    {
        Debug.Log("FallDownAndRefillBoard() started.");
        FindAllMatches();
        foreach (GameObject tile in tiles)
        {
            if (tile.GetComponent<Tile>().dirty && tile.GetComponent<Tile>().row < board.height - 1 &&
                !tiles[tile.GetComponent<Tile>().column, tile.GetComponent<Tile>().row + 1].GetComponent<Tile>().dirty)
            {
                for (int i = tile.GetComponent<Tile>().row + 1; i < board.height; i++)
                {
                    if (!tiles[tile.GetComponent<Tile>().column, i].GetComponent<Tile>().dirty)
                    {
                        DropColumn(tiles[tile.GetComponent<Tile>().column, i]);
                        break;
                    }
                }
            }
        }
        foreach (GameObject tile in tiles)
        {
            if (tile.GetComponent<Tile>().dirty)
            {
                if (tile.GetComponent<Tile>().ballSO.bonus != "none")
                {
                    if (tile.GetComponent<Tile>().ballSO.bonus == "ColorPunch")
                    {
                        ApplyColorPunchBonus(tile.GetComponent<Tile>().ballSO);
                    }
                    if (tile.GetComponent<Tile>().ballSO.bonus == "AllAround")
                    {
                        ApplyAllAroundBonus(tile);
                    }    
                }
                int randomBall = Random.Range(0, board.ballTypes.Length);
                tile.GetComponent<Tile>().ballSO = board.ballTypes[randomBall];
                tile.GetComponent<SpriteRenderer>().sprite = tile.GetComponent<Tile>().ballSO.sprite;
                tile.GetComponent<Tile>().dirty = false;
            }
        }
        FindAllMatches();
        Debug.Log("New matches after Fall Down: " + matchCount);
        while (matchCount > 0) { FallDownAndRefillBoard(); }
    }



    /*
    * @sourceTile first tile that needs to be dropped in a column
    * DropColumn identifies length of a drop balls in a column are supposed
    * to make and proceeds to interate over remaining balls in a column
    * starting with sourceTile dropping them by calculated length
    */
    private void DropColumn(GameObject sourceTile)
    {
        int droplength = 0;
        for (int i = sourceTile.GetComponent<Tile>().row - 1; i >= 0; i--)
        {
            droplength++;
            if (i > 0 && !tiles[sourceTile.GetComponent<Tile>().column, i-1].GetComponent<Tile>().dirty)
            {
                break;
            }
        }
        Debug.Log("DropColumn started. " + sourceTile.name + " by " + droplength +  " row(s)");
        for (int i = sourceTile.GetComponent<Tile>().row; i < board.height; i++)
        {
            sourceTile.GetComponent<Tile>().dirty = true;
            tiles[sourceTile.GetComponent<Tile>().column, i - droplength].GetComponent<Tile>().ballSO = sourceTile.GetComponent<Tile>().ballSO;
            tiles[sourceTile.GetComponent<Tile>().column, i - droplength].GetComponent<SpriteRenderer>().sprite = sourceTile.GetComponent<Tile>().ballSO.sprite;
            tiles[sourceTile.GetComponent<Tile>().column, i - droplength].GetComponent<Tile>().tileBallname = sourceTile.GetComponent<Tile>().tileBallname;
            tiles[sourceTile.GetComponent<Tile>().column, i - droplength].GetComponent<Tile>().dirty = false;
            if (i < board.height - 1) { sourceTile = tiles[sourceTile.GetComponent<Tile>().column, i + 1]; }
        }
    }

    /*
    * SwapTiles enables ability to move balls to different positions
    * And swaps them back if there is no match found.
    * Executes FallDownAndRefillBoard(); after match is found.
    */
    private void SwapTiles()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, -Vector2.up);
            if (hit.collider != null)
            {
                Cursor.visible = false;
                tmpTile = hit.collider.gameObject;
                tmpTile.GetComponent<CircleCollider2D>().enabled = false;
                tmpTransform = tmpTile.transform; 
                Debug.Log(tmpTransform.transform.position);
            }
        }
        if (Input.GetMouseButton(0) && tmpTile != null)
        {
            //Debug.Log(tmpTransform.transform.position);
            //z=-2 so it hovers over board - without that it worked according to the order of being added to the boardSO
            tmpTile.transform.position = new Vector3(mousePosition.x, mousePosition.y, -2);
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (tmpTile != null)
            {
                Cursor.visible = true;
                tmpTile.transform.position = tmpTile.GetComponent<Tile>().initialPosition;
                RaycastHit2D hit = Physics2D.Raycast(mousePosition, -Vector2.up);
                if (hit.collider != null && CheckMovementConstraints(hit.collider.gameObject))
                {
                    //swap tiles before match check
                    ball = hit.collider.gameObject.GetComponent<Tile>().ballSO;
                    hit.collider.gameObject.GetComponent<Tile>().ballSO = tmpTile.GetComponent<Tile>().ballSO;
                    tmpTile.GetComponent<Tile>().ballSO = ball;
                    //If no match swap back
                    if (!FindMatch(tmpTile) && !FindMatch(hit.collider.gameObject))
                    {
                        tmpTile.GetComponent<Tile>().ballSO = hit.collider.gameObject.GetComponent<Tile>().ballSO;
                        hit.collider.gameObject.GetComponent<Tile>().ballSO = ball;
                    } else
                    {
                        FallDownAndRefillBoard();
                    }
                }

            }
            tmpTile.GetComponent<CircleCollider2D>().enabled = true;
            tmpTile = null;
        }

    }

    /*
     * @targetTile tile that is being moved with dragndrop
     * CheckMovementConstraints locks ability to match tile with rules of match-3 games. 
     * Disable movementConstraint for easier testing. 
     */
    private bool CheckMovementConstraints(GameObject targetTile)
    {
        if (!movementConstraint)
        {
            return true;
        }
        else
        {
            if (tmpTile.GetComponent<Tile>().column <  board.width - 1 && tmpTile.GetComponent<Tile>().column + 1 == targetTile.GetComponent<Tile>().column &&
                tmpTile.GetComponent<Tile>().row == targetTile.GetComponent<Tile>().row) { return true; }
            if (tmpTile.GetComponent<Tile>().column > 0 && tmpTile.GetComponent<Tile>().column - 1 == targetTile.GetComponent<Tile>().column &&
                tmpTile.GetComponent<Tile>().row == targetTile.GetComponent<Tile>().row) { return true; }
            if (tmpTile.GetComponent<Tile>().row < board.height - 1 && tmpTile.GetComponent<Tile>().column == targetTile.GetComponent<Tile>().column &&
                tmpTile.GetComponent<Tile>().row + 1 == targetTile.GetComponent<Tile>().row) { return true; }
            if (tmpTile.GetComponent<Tile>().row > 0 && tmpTile.GetComponent<Tile>().column == targetTile.GetComponent<Tile>().column &&
                tmpTile.GetComponent<Tile>().row - 1 == targetTile.GetComponent<Tile>().row) { return true; }
            return false;
        }
    }

    /*
     * @sourceTile source Tile for ball colour
     * FindMatch checks if a ball after being moved to the new location becomes a match
     */
    private bool FindMatch(GameObject sourceTile)
    {
        //Debug.Log("FindMatch begin.");
        int column = sourceTile.GetComponent<Tile>().column;
        int row = sourceTile.GetComponent<Tile>().row;

        /*
         * Checking for the same balls on the right and the left
         */
        if (column > 0 && column < board.width - 1)
        {
            GameObject leftBall = tiles[column - 1, row];
            GameObject rightBall = tiles[column + 1, row];

            if (leftBall.GetComponent<Tile>().ballSO.ballName == sourceTile.GetComponent<Tile>().ballSO.ballName &&
                rightBall.GetComponent<Tile>().ballSO.ballName == sourceTile.GetComponent<Tile>().ballSO.ballName)
            {
                //Debug.Log("HORIZONTAL MATCH FOUND!");
                latestMatchFound = "horizontal";
                return true;
            }
        }
        /*
         * Checking for two same balls on the right
         */
        if (column < board.width - 2)
        {
            GameObject rightBall = tiles[column + 1, row];
            if (rightBall.GetComponent<Tile>().ballSO.ballName == sourceTile.GetComponent<Tile>().ballSO.ballName)
            {
                GameObject nextRightBall = tiles[column + 2, row];
                if (nextRightBall.GetComponent<Tile>().ballSO.ballName == sourceTile.GetComponent<Tile>().ballSO.ballName)
                {
                    //Debug.Log("HORIZONTAL MATCH FOUND! (2 right)");
                    latestMatchFound = "horizontal";
                    return true;
                }
            }
        }
        /*
         * Checking for the same balls on the left
         */
        if (column > 1)
        {
            GameObject leftBall = tiles[column - 1, row];
            if (leftBall.GetComponent<Tile>().ballSO.ballName == sourceTile.GetComponent<Tile>().ballSO.ballName)
            {
                GameObject nextLeftBall = tiles[column - 2, row];
                if (nextLeftBall.GetComponent<Tile>().ballSO.ballName == sourceTile.GetComponent<Tile>().ballSO.ballName)
                {
                    //Debug.Log("HORIZONTAL MATCH FOUND! (2 left)");
                    latestMatchFound = "horizontal";
                    return true;
                }
            }
        }
        /*
         * Checking for the same balls above and below
         */
        if (row > 0 && row < board.height - 1)
        {
            //Debug.Log("Middle Column check passed;");
            GameObject bottomBall = tiles[column, row - 1];
            GameObject topBall = tiles[column, row + 1];
            if (bottomBall.GetComponent<Tile>().ballSO.ballName == sourceTile.GetComponent<Tile>().ballSO.ballName &&
                topBall.GetComponent<Tile>().ballSO.ballName == sourceTile.GetComponent<Tile>().ballSO.ballName)
            {
                //Debug.Log("VERTICAL MATCH FOUND!");
                latestMatchFound = "vertical";
                return true;
            } 
        }
        /*
         * Checking for two same balls above
         */
        if (row < board.height - 2)
        {
            GameObject topBall = tiles[column, row + 1];
            if (topBall.GetComponent<Tile>().ballSO.ballName == sourceTile.GetComponent<Tile>().ballSO.ballName)
            {
                GameObject nextTopBall = tiles[column, row + 2];
                if (nextTopBall.GetComponent<Tile>().ballSO.ballName == sourceTile.GetComponent<Tile>().ballSO.ballName)
                {
                    //Debug.Log("VERTICAL MATCH FOUND! (2 top)");
                    latestMatchFound = "vertical";
                    return true;
                }
            }
        }
        /*
         * Checking for two same balls below
         */
        if (row > 1)
        {
            GameObject bottomBall = tiles[column, row - 1];
            if (bottomBall.GetComponent<Tile>().ballSO.ballName == sourceTile.GetComponent<Tile>().ballSO.ballName)
            {
                GameObject nextBottomBall = tiles[column, row - 2];
                if (nextBottomBall.GetComponent<Tile>().ballSO.ballName == sourceTile.GetComponent<Tile>().ballSO.ballName)
                {
                    //Debug.Log("VERTICAL MATCH FOUND! (2 bot)");
                    latestMatchFound = "vertical";
                    return true;
                }
            }
        }
        return false;
    }

    /*
      * FindAllMatches interates over tiles, checks if particular Tile is matched
      * and then dirties every tile that is part of that match
     */
    private void FindAllMatches()
    {
        //Debug.Log("FindAllMatches test begin. [0,0] " + FindMatch(tiles[0, 0]) + " " + latestMatchFound);
        matchCount = 0;
        foreach (GameObject tile in tiles)
        {
            if (FindMatch(tile))
            {
                matchCount++;
                //Debug.Log("FindAllMatches FindMatch true for 00. Match is: " + latestMatchFound);
                int column = tile.GetComponent<Tile>().column;
                int row = tile.GetComponent<Tile>().row;

                //Debug.Log("Match found in FindAllMatches for tile " + tile.name);
                if (latestMatchFound == "horizontal")
                {
                    //Debug.Log("Global horizontal match check.");
                    //check all balls left and right up until color is different
                    //dirty all balls of same colour as TILE
                    //100 % sure that there are graph/matrix algorithms much better for task done below, would've researched if optimalization was the main concern
                    for (int i = column; i < board.width; i++)
                        {
                            if (tiles[i, row].GetComponent<Tile>().ballSO.ballName == tile.GetComponent<Tile>().ballSO.ballName)
                            {
                                tiles[i, row].GetComponent<Tile>().dirty = true;
                            } 
                            else
                            {
                                break;
                            }
                        }
                    for (int j = column; j >= 0; j--)
                    {
                        if (tiles[j, row].GetComponent<Tile>().ballSO.ballName == tile.GetComponent<Tile>().ballSO.ballName)
                        {
                            tiles[j, row].GetComponent<Tile>().dirty = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                if (latestMatchFound == "vertical")
                {
                    //Debug.Log("Global horizontal match check.");
                    tile.GetComponent<Tile>().dirty = true;
                    //check all balls above and below up until color is different
                    //dirty all balls of same colour as TILE
                    for (int i = row; i < board.height; i++)
                    {
                        if (tiles[column, i].GetComponent<Tile>().ballSO.ballName == tile.GetComponent<Tile>().ballSO.ballName)
                        {
                            tiles[column, i].GetComponent<Tile>().dirty = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                    for (int j = row; j >= 0; j--)
                    {
                        if (tiles[column, j].GetComponent<Tile>().ballSO.ballName == tile.GetComponent<Tile>().ballSO.ballName)
                        {
                            tiles[column, j].GetComponent<Tile>().dirty = true;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

    }

    /*
     * @ballSO needed for color check
     * ApplyColorPunchBonus dirties every tile with a ball of the same colour 
     * as source ballSO
     */
    private void ApplyColorPunchBonus(BallSO ballSO)
    {
        foreach (GameObject tile in tiles)
        {
            if (tile.GetComponent<Tile>().ballSO.ballName == ballSO.ballName)
            {
                tile.GetComponent<Tile>().dirty = true;
            }
        }
    }

    /*
    * @tile tile with ball consisting AllAround bonus
    * ApplyAllAroundBonus dirties all the tiles around a source tile at range of 1 
    */
    private void ApplyAllAroundBonus(GameObject tile)
    {
        if (tile.GetComponent<Tile>().column < board.width - 1) { tiles[tile.GetComponent<Tile>().column + 1, tile.GetComponent<Tile>().row].GetComponent<Tile>().dirty = true; }
        if (tile.GetComponent<Tile>().column > 0) { tiles[tile.GetComponent<Tile>().column - 1, tile.GetComponent<Tile>().row].GetComponent<Tile>().dirty = true; }
        if (tile.GetComponent<Tile>().row < board.height - 1) { tiles[tile.GetComponent<Tile>().column, tile.GetComponent<Tile>().row + 1].GetComponent<Tile>().dirty = true; }
        if (tile.GetComponent<Tile>().row > 0) { tiles[tile.GetComponent<Tile>().column, tile.GetComponent<Tile>().row - 1].GetComponent<Tile>().dirty = true; }
    }
}
