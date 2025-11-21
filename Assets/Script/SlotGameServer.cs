using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class Position
{
    public int reel;
    public int row;

    public Position(int reel, int row)
    {
        this.reel = reel;
        this.row = row;
    }
}

[System.Serializable]
public class Payline
{
    public int line_number;
    public string[] symbols;
    public int count;
    public string symbol;
    public float win_amount;
    public int[][] positions;
}

[System.Serializable]
public class GameState
{
    public string client_id = "41";
    public string user_id;
    public string player_id = "48";
    public string game_id = "255";
    public string bet_id;
    public float bet_amount;
    public List<List<string>> reels;
    public Payline[] paylines;
    public float total_win;
    public bool is_free_spin = false;
    public int scatter_count = 0;
    public string timestamp;
}

[System.Serializable]
public class GameResponse
{
    public string status = "success";
    public string[][] reels;
    public Payline[] win_lines;
    public float total_win;
    public string message = "Spin complete";
    public bool is_free_spin = false;
    public float multiplier = 1f;
    public bool has_bonus = false;
}

public class SlotGameServer : MonoBehaviour
{
    [Header("RTP Settings")]
    [SerializeField, Range(50f, 95f)]
    private float targetRTP = 70f;

    [Header("Game Configuration")]
    [SerializeField]
    private int reelCount = 5;
    [SerializeField]
    private int rowCount = 3;

    // Game symbols
    private readonly string[] symbols = {
        "Bonus", "Wild", "10", "J", "Q", "K", "RedDragon", "GreenDragon", "BlackDragon", "WhiteDragon",
    };

    // Bet multipliers
    private readonly Dictionary<float, float> betMultipliers = new Dictionary<float, float>
    {
        { 0.25f, 1f },
        { 0.5f, 2f },
        { 1.25f, 5f },
        { 2.5f, 10f },
        { 6.25f, 20f }
    };

    // Paytable - symbol -> (5, 4, 3, 2 consecutive wins)
    private readonly Dictionary<string, float[]> paytable = new Dictionary<string, float[]>
    {
        { "RedDragon", new float[] { 3000f, 500f, 50f, 2f } },
        { "GreenDragon", new float[] { 1500f, 200f, 50f, 2f } },
        { "BlackDragon", new float[] { 200f, 60f, 10f, 0f } },
        { "WhiteDragon", new float[] { 200f, 60f, 10f, 0f } },
        { "K", new float[] { 150f, 30f, 10f, 0f } },
        { "Q", new float[] { 150f, 30f, 10f, 0f } },
        { "J", new float[] { 125f, 30f, 5f, 0f } },
        { "10", new float[] { 125f, 10f, 5f, 0f } },
        { "Wild", new float[] { 10000f, 3000f, 500f, 10f } }, // Wild pays on its own
        { "Bonus", new float[] { 0f, 0f, 0f, 0f } } // Bonus doesn't pay on its own
    };

    // All 25 paylines
    private readonly Position[][] paylinePositions = {
    // Line 1-3 (straight lines)
    new Position[] { new Position(0,1), new Position(1,1), new Position(2,1), new Position(3,1), new Position(4,1) },
    new Position[] { new Position(0,0), new Position(1,0), new Position(2,0), new Position(3,0), new Position(4,0) },
    new Position[] { new Position(0,2), new Position(1,2), new Position(2,2), new Position(3,2), new Position(4,2) },
    
    // Line 4-25 (various patterns)
    new Position[] { new Position(0,0), new Position(1,1), new Position(2,2), new Position(3,1), new Position(4,0) },
    new Position[] { new Position(0,2), new Position(1,1), new Position(2,0), new Position(3,1), new Position(4,2) },
    new Position[] { new Position(0,0), new Position(1,0), new Position(2,1), new Position(3,2), new Position(4,2) },
    new Position[] { new Position(0,2), new Position(1,2), new Position(2,1), new Position(3,0), new Position(4,0) },
    new Position[] { new Position(0,1), new Position(1,0), new Position(2,1), new Position(3,2), new Position(4,1) },
    new Position[] { new Position(0,1), new Position(1,2), new Position(2,1), new Position(3,0), new Position(4,1) },
    new Position[] { new Position(0,0), new Position(1,1), new Position(2,1), new Position(3,1), new Position(4,2) },
    new Position[] { new Position(0,2), new Position(1,1), new Position(2,1), new Position(3,1), new Position(4,0) },
    new Position[] { new Position(0,1), new Position(1,1), new Position(2,0), new Position(3,1), new Position(4,1) },
    new Position[] { new Position(0,1), new Position(1,1), new Position(2,2), new Position(3,1), new Position(4,1) },
    new Position[] { new Position(0,1), new Position(1,0), new Position(2,0), new Position(3,0), new Position(4,1) },
    new Position[] { new Position(0,1), new Position(1,2), new Position(2,2), new Position(3,2), new Position(4,1) },
    new Position[] { new Position(0,0), new Position(1,0), new Position(2,1), new Position(3,2), new Position(4,1) },
    new Position[] { new Position(0,2), new Position(1,2), new Position(2,1), new Position(3,0), new Position(4,1) },
    new Position[] { new Position(0,1), new Position(1,0), new Position(2,1), new Position(3,2), new Position(4,2) },
    new Position[] { new Position(0,1), new Position(1,2), new Position(2,1), new Position(3,0), new Position(4,0) },
    new Position[] { new Position(0,0), new Position(1,0), new Position(2,0), new Position(3,1), new Position(4,2) },
    new Position[] { new Position(0,2), new Position(1,2), new Position(2,2), new Position(3,1), new Position(4,0) },
    new Position[] { new Position(0,2), new Position(1,1), new Position(2,0), new Position(3,0), new Position(4,0) },
    new Position[] { new Position(0,0), new Position(1,1), new Position(2,2), new Position(3,2), new Position(4,2) },
    new Position[] { new Position(0,0), new Position(1,1), new Position(2,2), new Position(3,1), new Position(4,2) },
    new Position[] { new Position(0,2), new Position(1,1), new Position(2,0), new Position(3,1), new Position(4,0) }
};


    private System.Random random = new System.Random();

    public string SpinReels(float betAmount, bool isFreeSpins = false)
    {
        // Validate bet amount
        if (!betMultipliers.ContainsKey(betAmount))
        {
            Debug.LogError($"Invalid bet amount: {betAmount}. Valid amounts: {string.Join(", ", betMultipliers.Keys)}");
            return "{\"success\":false}";
        }

        // Generate reels
        string[][] reels = GenerateReels(isFreeSpins);
        Debug.Log("Generating Reels");

        // Check paylines
        List<Payline> winningPaylines = CheckPaylines(reels, betAmount);
        Debug.Log("Checking Winning Paylines");

        // Calculate total win
        float totalWin = winningPaylines.Sum(p => p.win_amount);
        Debug.Log("Calculating wins");

        return SerializeGameResponseNew(reels, winningPaylines.ToArray(), totalWin, isFreeSpins, betAmount);
    }

    private string SerializeGameResponseNew(string[][] reels, Payline[] winLines, float totalWin, bool isFreeSpins, float betAmount)
    {
        GameResponse response = new GameResponse
        {
            status = "success",
            reels = reels,
            win_lines = winLines,
            total_win = totalWin,
            is_free_spin = isFreeSpins,
            message = totalWin > 0 ? "Win!" : "No win",
            multiplier = betMultipliers[betAmount],
            has_bonus = winLines.Any(p => p.symbols.Contains("Bonus"))
        };

        return Newtonsoft.Json.JsonConvert.SerializeObject(response);
    }

    // private string SerializeGameResponse(GameState gameState)
    // {
    //     // Use CultureInfo.InvariantCulture to ensure '.' is used as the decimal separator.
    //     var culture = CultureInfo.InvariantCulture;

    //     StringBuilder json = new StringBuilder();
    //     json.Append("{");
    //     json.Append("\"success\":true,");
    //     json.Append("\"game_state\":{");

    //     // Basic fields
    //     json.AppendFormat(culture, "\"client_id\":\"{0}\",", gameState.client_id);
    //     json.AppendFormat(culture, "\"user_id\":\"{0}\",", gameState.user_id);
    //     json.AppendFormat(culture, "\"player_id\":\"{0}\",", gameState.player_id);
    //     json.AppendFormat(culture, "\"game_id\":\"{0}\",", gameState.game_id);
    //     json.AppendFormat(culture, "\"bet_id\":\"{0}\",", gameState.bet_id);
    //     // MODIFIED: Added CultureInfo.InvariantCulture to format numbers correctly for JSON.
    //     json.AppendFormat(culture, "\"bet_amount\":{0},", gameState.bet_amount);

    //     // Reels array
    //     json.Append("\"reels\":[");
    //     for (int i = 0; i < gameState.reels.Count; i++)
    //     {
    //         json.Append("[");
    //         for (int j = 0; j < gameState.reels[i].Count; j++)
    //         {
    //             json.AppendFormat(culture, "\"{0}\"", gameState.reels[i][j]);
    //             if (j < gameState.reels[i].Count - 1) json.Append(",");
    //         }
    //         json.Append("]");
    //         if (i < gameState.reels.Count - 1) json.Append(",");
    //     }
    //     json.Append("],");

    //     // Paylines array
    //     json.Append("\"paylines\":[");
    //     for (int i = 0; i < gameState.paylines.Length; i++)
    //     {
    //         var payline = gameState.paylines[i];
    //         json.Append("{");
    //         json.AppendFormat(culture, "\"line\":{0},", payline.line_number);

    //         // Symbols array
    //         json.Append("\"symbols\":[");
    //         for (int j = 0; j < payline.symbols.Length; j++)
    //         {
    //             json.AppendFormat(culture, "\"{0}\"", payline.symbols[j]);
    //             if (j < payline.symbols.Length - 1) json.Append(",");
    //         }
    //         json.Append("],");

    //         json.AppendFormat(culture, "\"count\":{0},", payline.count);
    //         json.AppendFormat(culture, "\"symbol\":\"{0}\",", payline.symbol);
    //         // MODIFIED: Added CultureInfo.InvariantCulture to format numbers correctly for JSON.
    //         json.AppendFormat(culture, "\"payout\":{0},", payline.win_amount);

    //         // Positions array
    //         json.Append("\"positions\":[");
    //         for (int j = 0; j < payline.positions.Length; j++)
    //         {
    //             var pos = payline.positions[j];
    //             json.AppendFormat(culture, "{{\"reel\":{0},\"row\":{1}}}", pos.reel, pos.row);
    //             if (j < payline.positions.Length - 1) json.Append(",");
    //         }
    //         json.Append("]");

    //         json.Append("}");
    //         if (i < gameState.paylines.Length - 1) json.Append(",");
    //     }
    //     json.Append("],");

    //     // Remaining fields
    //     // MODIFIED: Added CultureInfo.InvariantCulture to format numbers correctly for JSON.
    //     json.AppendFormat(culture, "\"total_win\":{0},", gameState.total_win);
    //     json.AppendFormat(culture, "\"is_free_spin\":{0},", gameState.is_free_spin.ToString().ToLower());
    //     json.AppendFormat(culture, "\"scatter_count\":{0},", gameState.scatter_count);
    //     json.AppendFormat(culture, "\"timestamp\":\"{0}\"", gameState.timestamp);

    //     json.Append("}}");
    //     return json.ToString();
    // }

    private string[][] GenerateReels(bool isFreeSpins = false)
    {
        string[][] reels = new string[reelCount][];

        for (int reel = 0; reel < reelCount; reel++)
        {
            reels[reel] = new string[rowCount];
            for (int row = 0; row < rowCount; row++)
            {
                reels[reel][row] = GetRandomSymbol(reel, isFreeSpins);
            }
        }

        // If it's free spins, ensure reels 2,3,4,5 have at least 1 Wild each
        if (isFreeSpins)
        {
            for (int reel = 1; reel < reelCount; reel++) // Start from reel 0 (which is reel 1)
            {
                EnsureWildInReel(reels[reel]);
            }
        }

        return reels;
    }

    private void EnsureWildInReel(string[] reel)
    {
        // Check if reel already has at least one Wild
        bool hasWild = reel.Any(symbol => symbol == "Wild");

        if (!hasWild)
        {
            // Replace a random symbol with Wild
            int randomPosition = random.Next(reel.Length);
            reel[randomPosition] = "Wild";
        }
    }

    private string GetRandomSymbol(int reelIndex, bool isFreeSpins = false)
    {
        // Weighted symbol selection based on RTP
        // Higher RTP means more frequent low-paying symbols
        float rtpFactor = targetRTP / 100f;

        // Weight distribution (can be fine-tuned for better RTP control)
        float[] weights = new float[symbols.Length];

        // Lower value symbols get higher weights for lower RTP
        weights[Array.IndexOf(symbols, "10")] = 1.0f - (rtpFactor * 0.3f);
        weights[Array.IndexOf(symbols, "J")] = 1.0f - (rtpFactor * 0.3f);
        weights[Array.IndexOf(symbols, "Q")] = 1.0f - (rtpFactor * 0.3f);
        weights[Array.IndexOf(symbols, "K")] = 0.8f - (rtpFactor * 0.2f);
        weights[Array.IndexOf(symbols, "WhiteDragon")] = 0.6f - (rtpFactor * 0.15f);
        weights[Array.IndexOf(symbols, "BlackDragon")] = 0.6f - (rtpFactor * 0.15f);
        weights[Array.IndexOf(symbols, "GreenDragon")] = 0.4f - (rtpFactor * 0.1f);
        weights[Array.IndexOf(symbols, "RedDragon")] = 0.2f - (rtpFactor * 0.05f);
        weights[Array.IndexOf(symbols, "Bonus")] = 0.1f;
        weights[Array.IndexOf(symbols, "Wild")] = 0.3f - (rtpFactor * 0.1f);
        // Wild symbol only appears in reels 2, 3, 4, and 5 (indices 1, 2, 3, 4)
        // if (reelIndex == 0)
        // {
        //     weights[Array.IndexOf(symbols, "Wild")] = 0f; // No Wild in first reel
        // }
        // else
        // {
        //     // During free spins, significantly increase Wild weight in reels 2,3,4,5
        //     if (isFreeSpins)
        //     {
        //         weights[Array.IndexOf(symbols, "Wild")] = 2.0f; // Much higher weight for free spins
        //     }
        //     else
        //     {
        //     }
        // }

        return GetWeightedRandomSymbol(weights);
    }

    private string GetWeightedRandomSymbol(float[] weights)
    {
        float totalWeight = weights.Sum();
        float randomValue = (float)random.NextDouble() * totalWeight;

        float currentWeight = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
                return symbols[i];
        }

        return symbols[0]; // Fallback
    }

    private List<Payline> CheckPaylines(string[][] reels, float betAmount)
    {
        List<Payline> winningPaylines = new List<Payline>();
        float multiplier = betMultipliers[betAmount];

        for (int lineIndex = 0; lineIndex < paylinePositions.Length; lineIndex++)
        {
            Position[] positions = paylinePositions[lineIndex];
            string[] lineSymbols = new string[5];

            // Get symbols for this payline
            for (int i = 0; i < positions.Length; i++)
            {
                lineSymbols[i] = reels[positions[i].reel][positions[i].row];
            }

            // Check for winning combinations
            Payline winningLine = CheckLineForWin(lineIndex + 1, lineSymbols, positions, multiplier);
            if (winningLine != null)
            {
                winningPaylines.Add(winningLine);
            }
        }

        return winningPaylines;
    }

    private Payline CheckLineForWin(int lineNumber, string[] lineSymbols, Position[] positions, float multiplier)
    {
        string firstSymbol = lineSymbols[0];
        if (firstSymbol == "Bonus" || firstSymbol == "Wild") return null;

        int consecutiveCount = 1;
        string winSymbol = firstSymbol;

        // Check consecutive symbols from left to right
        for (int i = 1; i < lineSymbols.Length; i++)
        {
            string currentSymbol = lineSymbols[i];

            // Wild can substitute for any symbol except Bonus
            if (currentSymbol == "Wild" || currentSymbol == winSymbol)
            {
                consecutiveCount++;
            }
            else
            {
                break;
            }
        }

        // Check if we have a winning combination
        bool isWin = false;
        if (winSymbol == "RedDragon" && consecutiveCount >= 2)
            isWin = true;
        if (winSymbol == "GreenDragon" && consecutiveCount >= 2)
            isWin = true;
        else if (consecutiveCount >= 3)
            isWin = true;

        if (isWin && paytable.ContainsKey(winSymbol))
        {
            float[] payouts = paytable[winSymbol];
            float basePayout = 0f;

            // Get payout based on consecutive count
            switch (consecutiveCount)
            {
                case 5: basePayout = payouts[0]; break;
                case 4: basePayout = payouts[1]; break;
                case 3: basePayout = payouts[2]; break;
                case 2: basePayout = payouts[3]; break;
            }

            if (basePayout > 0)
            {
                float finalPayout = basePayout * 0.01f * multiplier;
                
                // Convert Position[] to int[][]
                int[][] positionsArray = new int[positions.Length][];
                for (int i = 0; i < positions.Length; i++)
                {
                    positionsArray[i] = new int[] { positions[i].reel, positions[i].row };
                }

                return new Payline
                {
                    line_number = lineNumber,
                    symbols = lineSymbols,
                    count = consecutiveCount,
                    symbol = winSymbol,
                    win_amount = finalPayout,
                    positions = positionsArray
                };
            }
        }

        return null;
    }

    // Public method to adjust RTP during runtime
    public void SetRTP(float newRTP)
    {
        targetRTP = Mathf.Clamp(newRTP, 50f, 95f);
        Debug.Log($"RTP set to: {targetRTP}%");
    }

    // Test methods
    [ContextMenu("Test Normal Spin")]
    public void TestNormalSpin()
    {
        string jsonResult = SpinReels(0.25f, false);
        Debug.Log("Normal Spin Result JSON:");
        Debug.Log(jsonResult);
    }

    [ContextMenu("Test Free Spin")]
    public void TestFreeSpin()
    {
        string jsonResult = SpinReels(0.25f, true);
        Debug.Log("Free Spin Result JSON:");
        Debug.Log(jsonResult);
    }
}