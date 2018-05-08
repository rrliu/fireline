using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(HexGrid))]
public class TurnScript : MonoBehaviour
{
    public GameObject nextTurnText;
    public GameObject nextTurnPrompt;
    public Text moneyText;
    public Text incomeText;
    public int money;
    [HideInInspector] public bool playerTurn;

    HexGrid hexGrid;
    bool turnTransition = false;

    void UpdateIncomeText(int income) {
        incomeText.text = "(+ $ " + income.ToString() + ")";
    }

    void UpdateMoneyText() {
        moneyText.text = "$ " + money.ToString();
    }

    int GetTileProfit(Vector2Int tile) {
        TileInfo tileInfo = hexGrid.tiles[tile.x, tile.y];
        TileType tileType = tileInfo.type;
        if (tileInfo.fire == null && !tileInfo.disabled) {
            if (tileType == TileType.CITY) {
                return 3;
            }
            if (tileType == TileType.FOREST || tileType == TileType.GRASSLAND) {
                return 1;
            }
        }

        return 0;
    }

    int CalcIncome() {
        int income = 0;
        int width = hexGrid.tiles.GetLength(0);
        int height = hexGrid.tiles.GetLength(1);
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                Vector2Int tile = new Vector2Int(i, j);
                income += GetTileProfit(tile);
            }
        }
        return income;
    }

    void StartPlayerTurn() {
        int width = hexGrid.tiles.GetLength(0);
        int height = hexGrid.tiles.GetLength(1);
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                if (hexGrid.tiles[i, j].unit != null) {
                    hexGrid.tiles[i, j].unitScript.rangeRemaining =
						hexGrid.tiles[i, j].unitScript.range;
                }
            }
        }
    }

    [HideInInspector] public bool doneWithUnits;
    // TODO hacky af
    IEnumerator NextTurnCoroutine() {
        turnTransition = true;
        nextTurnText.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        // Make money
        money += CalcIncome();
        UpdateMoneyText();

        // Units
        doneWithUnits = false;
        StartCoroutine(hexGrid.ExecuteUnitCommands());
        while (!doneWithUnits) {
            yield return null;
        }

        // Fire
        hexGrid.AgeFire();
        hexGrid.SpreadFire();

        // Update unit command paths after fire has spread
        hexGrid.UpdateUnitCommands();

        UpdateIncomeText(CalcIncome());

        nextTurnText.SetActive(false);
        playerTurn = true;
        StartPlayerTurn();
        turnTransition = false;
    }

    // Use this for initialization
    void Start() {
        hexGrid = GetComponent<HexGrid>();
        UpdateMoneyText();
        UpdateIncomeText(CalcIncome());
        playerTurn = true;
    }
	
    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            if (playerTurn) {
                playerTurn = false;
                //Debug.Log ("End turn");
            }

            if (!playerTurn && !turnTransition) {
                StartCoroutine(NextTurnCoroutine());
            }
        }
    }
}
