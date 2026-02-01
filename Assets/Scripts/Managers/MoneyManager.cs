using UnityEngine;
using System;

namespace Maskhot.Managers
{
    /// <summary>
    /// Singleton manager that owns player currency state.
    ///
    /// Responsibilities:
    /// - Track player balance
    /// - Execute spend/add transactions
    /// - Fire events on balance changes
    ///
    /// Does NOT handle:
    /// - Determining costs for specific actions (Controller)
    /// - UI feedback for failed transactions (Controller)
    /// </summary>
    public class MoneyManager : MonoBehaviour
    {
        public static MoneyManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Starting balance for the player")]
        public int startingBalance = 500;

        [Header("Debug")]
        [Tooltip("Enable detailed logging")]
        public bool verboseLogging = false;

        /// <summary>
        /// Event fired when the balance changes.
        /// Parameter: the new balance
        /// </summary>
        public event Action<int> OnBalanceChanged;

        private int currentBalance;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            currentBalance = startingBalance;

            if (verboseLogging)
            {
                Debug.Log($"MoneyManager: Initialized with balance ${currentBalance}");
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the player's current balance.
        /// </summary>
        public int CurrentBalance => currentBalance;

        /// <summary>
        /// Returns the configured starting balance.
        /// </summary>
        public int StartingBalance => startingBalance;

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if the player can afford a given cost.
        /// </summary>
        /// <param name="cost">The cost to check</param>
        /// <returns>True if balance >= cost</returns>
        public bool CanAfford(int cost)
        {
            if (cost < 0)
            {
                if (verboseLogging)
                {
                    Debug.LogWarning("MoneyManager: CanAfford called with negative cost");
                }
                return false;
            }

            return currentBalance >= cost;
        }

        /// <summary>
        /// Attempts to spend a given amount.
        /// Fires OnBalanceChanged if successful.
        /// </summary>
        /// <param name="cost">The amount to spend</param>
        /// <returns>True if the transaction succeeded, false if insufficient funds or invalid cost</returns>
        public bool Spend(int cost)
        {
            if (cost < 0)
            {
                if (verboseLogging)
                {
                    Debug.LogWarning("MoneyManager: Spend called with negative cost");
                }
                return false;
            }

            if (cost == 0)
            {
                if (verboseLogging)
                {
                    Debug.Log("MoneyManager: Spend called with zero cost, no change");
                }
                return true;
            }

            if (!CanAfford(cost))
            {
                if (verboseLogging)
                {
                    Debug.Log($"MoneyManager: Cannot afford ${cost} (balance: ${currentBalance})");
                }
                return false;
            }

            currentBalance -= cost;

            if (verboseLogging)
            {
                Debug.Log($"MoneyManager: Spent ${cost}, new balance: ${currentBalance}");
            }

            OnBalanceChanged?.Invoke(currentBalance);
            return true;
        }

        /// <summary>
        /// Adds money to the player's balance.
        /// Fires OnBalanceChanged.
        /// </summary>
        /// <param name="amount">The amount to add</param>
        /// <returns>True if the transaction succeeded, false if invalid amount</returns>
        public bool AddMoney(int amount)
        {
            if (amount < 0)
            {
                if (verboseLogging)
                {
                    Debug.LogWarning("MoneyManager: AddMoney called with negative amount, use Spend instead");
                }
                return false;
            }

            if (amount == 0)
            {
                if (verboseLogging)
                {
                    Debug.Log("MoneyManager: AddMoney called with zero amount, no change");
                }
                return true;
            }

            currentBalance += amount;

            if (verboseLogging)
            {
                Debug.Log($"MoneyManager: Added ${amount}, new balance: ${currentBalance}");
            }

            OnBalanceChanged?.Invoke(currentBalance);
            return true;
        }

        /// <summary>
        /// Resets balance to the starting amount.
        /// Fires OnBalanceChanged.
        /// </summary>
        public void ResetBalance()
        {
            int previousBalance = currentBalance;
            currentBalance = startingBalance;

            if (verboseLogging)
            {
                Debug.Log($"MoneyManager: Reset balance from ${previousBalance} to ${currentBalance}");
            }

            OnBalanceChanged?.Invoke(currentBalance);
        }

        #endregion
    }
}
