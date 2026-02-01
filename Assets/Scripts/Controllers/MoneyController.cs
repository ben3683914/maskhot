using UnityEngine;
using System;
using Maskhot.Managers;

namespace Maskhot.Controllers
{
    /// <summary>
    /// Singleton controller that provides a UI-facing interface for player money.
    /// Subscribes to MoneyManager events and provides display-ready methods.
    /// UI should use this instead of accessing MoneyManager directly.
    ///
    /// Responsibilities:
    /// - Provide pass-through accessors for UI
    /// - Define unredact cost configuration
    /// - Validate and execute unredact purchases
    /// - Fire events for UI updates (including spend failures)
    /// </summary>
    public class MoneyController : MonoBehaviour
    {
        public static MoneyController Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Cost to unredact a single post")]
        public int unredactCost = 50;

        [Header("Debug")]
        [Tooltip("Enable detailed logging")]
        public bool verboseLogging = false;

        #region Events

        /// <summary>
        /// Fired when the balance changes.
        /// Parameter: the new balance
        /// </summary>
        public event Action<int> OnBalanceChanged;

        /// <summary>
        /// Fired when a spend attempt fails due to insufficient funds.
        /// Parameter: the cost that couldn't be afforded
        /// </summary>
        public event Action<int> OnSpendFailed;

        #endregion

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

            if (verboseLogging)
            {
                Debug.Log($"MoneyController: Initialized with unredact cost ${unredactCost}");
            }
        }

        private void OnEnable()
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.OnBalanceChanged += HandleBalanceChanged;
            }
        }

        private void OnDisable()
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.OnBalanceChanged -= HandleBalanceChanged;
            }
        }

        private void Start()
        {
            // Re-subscribe in case MoneyManager wasn't ready in OnEnable
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.OnBalanceChanged -= HandleBalanceChanged;
                MoneyManager.Instance.OnBalanceChanged += HandleBalanceChanged;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Returns the player's current balance.
        /// </summary>
        public int CurrentBalance
        {
            get
            {
                if (MoneyManager.Instance != null)
                {
                    return MoneyManager.Instance.CurrentBalance;
                }
                return 0;
            }
        }

        /// <summary>
        /// Returns the configured unredact cost.
        /// </summary>
        public int UnredactCost => unredactCost;

        #endregion

        #region Public Methods - Affordability

        /// <summary>
        /// Checks if the player can afford a given cost.
        /// </summary>
        /// <param name="cost">The cost to check</param>
        /// <returns>True if balance >= cost</returns>
        public bool CanAfford(int cost)
        {
            if (MoneyManager.Instance != null)
            {
                return MoneyManager.Instance.CanAfford(cost);
            }
            return false;
        }

        /// <summary>
        /// Checks if the player can afford to unredact a post.
        /// </summary>
        /// <returns>True if the player can afford the unredact cost</returns>
        public bool CanAffordUnredact()
        {
            return CanAfford(unredactCost);
        }

        #endregion

        #region Public Methods - Transactions

        /// <summary>
        /// Attempts to spend the unredact cost.
        /// Fires OnSpendFailed if insufficient funds.
        /// Does NOT actually unredact the post - caller should handle that via RedactionController.
        /// </summary>
        /// <returns>True if the spend succeeded, false if insufficient funds</returns>
        public bool TrySpendForUnredact()
        {
            if (MoneyManager.Instance == null)
            {
                if (verboseLogging)
                {
                    Debug.LogWarning("MoneyController: TrySpendForUnredact called but MoneyManager not available");
                }
                OnSpendFailed?.Invoke(unredactCost);
                return false;
            }

            if (!MoneyManager.Instance.CanAfford(unredactCost))
            {
                if (verboseLogging)
                {
                    Debug.Log($"MoneyController: Cannot afford unredact cost ${unredactCost} (balance: ${CurrentBalance})");
                }
                OnSpendFailed?.Invoke(unredactCost);
                return false;
            }

            bool success = MoneyManager.Instance.Spend(unredactCost);

            if (verboseLogging)
            {
                if (success)
                {
                    Debug.Log($"MoneyController: Spent ${unredactCost} for unredact, new balance: ${CurrentBalance}");
                }
                else
                {
                    Debug.LogWarning($"MoneyController: Spend failed unexpectedly");
                }
            }

            return success;
        }

        /// <summary>
        /// Attempts to spend a given amount.
        /// Fires OnSpendFailed if insufficient funds.
        /// </summary>
        /// <param name="cost">The amount to spend</param>
        /// <returns>True if the spend succeeded, false if insufficient funds</returns>
        public bool TrySpend(int cost)
        {
            if (MoneyManager.Instance == null)
            {
                if (verboseLogging)
                {
                    Debug.LogWarning("MoneyController: TrySpend called but MoneyManager not available");
                }
                OnSpendFailed?.Invoke(cost);
                return false;
            }

            if (!MoneyManager.Instance.CanAfford(cost))
            {
                if (verboseLogging)
                {
                    Debug.Log($"MoneyController: Cannot afford ${cost} (balance: ${CurrentBalance})");
                }
                OnSpendFailed?.Invoke(cost);
                return false;
            }

            bool success = MoneyManager.Instance.Spend(cost);

            if (verboseLogging && success)
            {
                Debug.Log($"MoneyController: Spent ${cost}, new balance: ${CurrentBalance}");
            }

            return success;
        }

        /// <summary>
        /// Adds money to the player's balance.
        /// Delegates to MoneyManager.
        /// </summary>
        /// <param name="amount">The amount to add</param>
        /// <returns>True if the transaction succeeded</returns>
        public bool AddMoney(int amount)
        {
            if (MoneyManager.Instance == null)
            {
                if (verboseLogging)
                {
                    Debug.LogWarning("MoneyController: AddMoney called but MoneyManager not available");
                }
                return false;
            }

            bool success = MoneyManager.Instance.AddMoney(amount);

            if (verboseLogging && success)
            {
                Debug.Log($"MoneyController: Added ${amount}, new balance: ${CurrentBalance}");
            }

            return success;
        }

        #endregion

        #region Public Methods - Display

        /// <summary>
        /// Formats the current balance for display.
        /// </summary>
        /// <returns>Formatted balance string (e.g., "$500")</returns>
        public string GetFormattedBalance()
        {
            return $"${CurrentBalance}";
        }

        /// <summary>
        /// Formats the unredact cost for display.
        /// </summary>
        /// <returns>Formatted cost string (e.g., "$50")</returns>
        public string GetFormattedUnredactCost()
        {
            return $"${unredactCost}";
        }

        #endregion

        #region Event Handlers

        private void HandleBalanceChanged(int newBalance)
        {
            if (verboseLogging)
            {
                Debug.Log($"MoneyController: Received OnBalanceChanged, new balance: ${newBalance}");
            }

            OnBalanceChanged?.Invoke(newBalance);
        }

        #endregion
    }
}
