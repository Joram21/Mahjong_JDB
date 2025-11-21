using System;
using System.Collections.Generic;
using System.ComponentModel;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public class LineManager : SerializedMonoBehaviour
{
    public event Action<int, int, SymbolData> OnGridInitialized;
    public static LineManager instance;

    public GameObject linesParent;
    
    public Line[] lines;
    
    // 2D array: 4 rows, 5 columns
    [SerializeField]
    public Symbol[,] VisibleSymbolsGrid = new Symbol[5, 3];
    [SerializeField]
    private Dictionary<(int col, int row), List<WinSquare>> positionToSquares;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        VisibleSymbolsGrid = new Symbol[5, 3];
        ResetLines();
        BuildPositionLookup();
    }

    public void ResetLines()
    {
        TurnOffAllSquaresInsideLines();
        TurnOffAllLines();
        linesParent.SetActive(false);
    }

    // Initialize this dictionary once when setting up your lines
    private void BuildPositionLookup()
    {
        positionToSquares = new Dictionary<(int, int), List<WinSquare>>();
    
        foreach (var line in lines)
        {
            foreach (var square in line.winSquaresParent.winSquares)
            {
                if (!positionToSquares.ContainsKey(square.GetPositions()))
                    positionToSquares[square.GetPositions()] = new List<WinSquare>();
                
                positionToSquares[square.GetPositions()].Add(square);
            }
        }
    }

    public void TurnOffAllSquaresInsideLines()
    {
        foreach (var line in lines)
        {
            foreach (var square in line.winSquaresParent.winSquares)
            {
                square.gameObject.SetActive(false);
            }
        }
    }
    
    public Symbol[,] CopyVisibleSymbolsGrid()
    {
        int rows = VisibleSymbolsGrid.GetLength(0);
        int cols = VisibleSymbolsGrid.GetLength(1);
        Symbol[,] copy = new Symbol[rows, cols];
    
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                copy[i, j] = VisibleSymbolsGrid[i, j];
            }
        }
    
        return copy;
    }
    
    public void TurnOffAllLines()
    {
        foreach (var line in lines)
        {
            line.gameObject.SetActive(false);
        }
    }
    
    public void AssignBg(bool isInFreeSpinMode)
    {
        foreach (var line in lines)
        {
            line.SetFreeSpinBgSprite(isInFreeSpinMode);
        }
    }
    
    public void TurnOnAllSquaresInsideLines()
    {
        foreach (var line in lines)
        {
            foreach (var square in line.winSquaresParent.winSquares)
            {
                square.gameObject.SetActive(true);
            }
        }
    }
    
    public void InitializeGrid(int row, int col, Symbol symbol, SymbolData symbolData)
    {
        VisibleSymbolsGrid[row, col] = symbol;
        VisibleSymbolsGrid[row, col].Initialize(symbolData);
        
        if (positionToSquares.TryGetValue((col, row), out var relevantSquares))
        {
            foreach (var square in relevantSquares)
            {
                square.HandleGridInitialized(symbolData);
            }
        }
    }
    
    // Method to get a specific element
    public Symbol GetElement(int row, int col)
    {
        if (IsValidPosition(row, col))
        {
            return VisibleSymbolsGrid[row, col];
        }
        
        // Debug.LogWarning($"Invalid position: ({row}, {col})");
        return null; // or throw an exception
    }
    
    // Method to set a specific element
    public void SetElement(int row, int col, Symbol value)
    {
        if (IsValidPosition(row, col))
        {
            VisibleSymbolsGrid[row, col] = value;
        }
        else
        {
            // Debug.LogWarning($"Invalid position: ({row}, {col})");
        }
    }
    
    // Method to get an entire row
    public Symbol[] GetRow(int row)
    {
        if (row >= 0 && row < 3)
        {
            Symbol[] rowData = new Symbol[5];
            for (int col = 0; col < 5; col++)
            {
                rowData[col] = VisibleSymbolsGrid[row, col];
            }
            return rowData;
        }
        
        // Debug.LogWarning($"Invalid row: {row}");
        return null;
    }
    
    // Method to get an entire column
    public Symbol[] GetColumn(int col)
    {
        if (col >= 0 && col < 5)
        {
            Symbol[] colData = new Symbol[4];
            for (int row = 0; row < 4; row++)
            {
                colData[row] = VisibleSymbolsGrid[row, col];
            }
            return colData;
        }
        
        // Debug.LogWarning($"Invalid column: {col}");
        return null;
    }
    
    // Method to check if position is valid
    private bool IsValidPosition(int row, int col)
    {
        return row >= 0 && row <3 && col >= 0 && col < 5;
    }
    
    // Method to display the entire grid (for debugging)
    public void DisplayGrid(int row, int col)
    {
        string gridString = "Grid Contents: ";
        gridString += VisibleSymbolsGrid[row, col].Data.type + " ";
        gridString += "\n";
        // Debug.Log(gridString);
    }
}