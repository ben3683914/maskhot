using UnityEngine;
using System.Text;
using Maskhot.Managers;
using Maskhot.Controllers;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify MoneyController functionality.
    ///
    /// Setup:
    /// 1. Attach to a GameObject alongside MoneyManager and MoneyController
    /// 2. Enter Play Mode
    ///
    /// How to Run: Click the buttons in the Inspector (enabled during Play Mode)
    ///
    /// What to Verify:
    /// - CurrentBalance pass-through works
    /// - CanAffordUnredact checks against unredact cost
    /// - TrySpendForUnredact deducts correct amount
    /// - OnSpendFailed fires when insufficient funds
    /// - OnBalanceChanged fires from controller
    /// </summary>
    public class MoneyControllerTester : MonoBehaviour
    {
        [Header("Options")]
        [Tooltip("Enable detailed logging")]
        public bool verboseOutput = false;

        private bool balanceChangedEventReceived = false;
        private int lastBalanceFromEvent = -1;
        private bool spendFailedEventReceived = false;
        private int lastFailedCost = -1;

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void Start()
        {
            // Re-subscribe in case controller wasn't ready
            UnsubscribeFromEvents();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (MoneyController.Instance != null)
            {
                MoneyController.Instance.OnBalanceChanged += HandleBalanceChanged;
                MoneyController.Instance.OnSpendFailed += HandleSpendFailed;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (MoneyController.Instance != null)
            {
                MoneyController.Instance.OnBalanceChanged -= HandleBalanceChanged;
                MoneyController.Instance.OnSpendFailed -= HandleSpendFailed;
            }
        }

        private void HandleBalanceChanged(int newBalance)
        {
            balanceChangedEventReceived = true;
            lastBalanceFromEvent = newBalance;

            if (verboseOutput)
            {
                Debug.Log($"MoneyControllerTester: OnBalanceChanged fired with balance ${newBalance}");
            }
        }

        private void HandleSpendFailed(int cost)
        {
            spendFailedEventReceived = true;
            lastFailedCost = cost;

            if (verboseOutput)
            {
                Debug.Log($"MoneyControllerTester: OnSpendFailed fired for cost ${cost}");
            }
        }

        /// <summary>
        /// Tests controller configuration and pass-through
        /// </summary>
        public void TestConfiguration()
        {
            if (!ValidateComponents()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY CONTROLLER TESTER: Configuration ===");
            sb.AppendLine();

            sb.AppendLine("--- CONTROLLER CONFIG ---");
            sb.AppendLine($"  Unredact Cost: ${MoneyController.Instance.UnredactCost}");
            sb.AppendLine();

            sb.AppendLine("--- PASS-THROUGH CHECK ---");
            int controllerBalance = MoneyController.Instance.CurrentBalance;
            int managerBalance = MoneyManager.Instance.CurrentBalance;
            sb.AppendLine($"  Controller.CurrentBalance: ${controllerBalance}");
            sb.AppendLine($"  Manager.CurrentBalance: ${managerBalance}");
            sb.AppendLine($"  Match: {controllerBalance == managerBalance}");
            sb.AppendLine();

            sb.AppendLine("--- FORMATTED OUTPUT ---");
            sb.AppendLine($"  GetFormattedBalance(): {MoneyController.Instance.GetFormattedBalance()}");
            sb.AppendLine($"  GetFormattedUnredactCost(): {MoneyController.Instance.GetFormattedUnredactCost()}");
            sb.AppendLine();

            bool passed = controllerBalance == managerBalance;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests CanAffordUnredact method
        /// </summary>
        public void TestCanAffordUnredact()
        {
            if (!ValidateComponents()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY CONTROLLER TESTER: CanAffordUnredact ===");
            sb.AppendLine();

            // Reset to known state
            MoneyManager.Instance.ResetBalance();

            int balance = MoneyController.Instance.CurrentBalance;
            int unredactCost = MoneyController.Instance.UnredactCost;

            sb.AppendLine($"  Current Balance: ${balance}");
            sb.AppendLine($"  Unredact Cost: ${unredactCost}");
            sb.AppendLine();

            sb.AppendLine("--- WITH SUFFICIENT FUNDS ---");
            bool canAfford = MoneyController.Instance.CanAffordUnredact();
            bool expected = balance >= unredactCost;
            sb.AppendLine($"  CanAffordUnredact(): {canAfford} (expected: {expected})");
            sb.AppendLine();

            // Drain balance to test insufficient funds
            while (MoneyManager.Instance.CurrentBalance >= unredactCost)
            {
                MoneyManager.Instance.Spend(unredactCost);
            }

            int lowBalance = MoneyController.Instance.CurrentBalance;
            sb.AppendLine("--- WITH INSUFFICIENT FUNDS ---");
            sb.AppendLine($"  Balance reduced to: ${lowBalance}");
            bool canAffordNow = MoneyController.Instance.CanAffordUnredact();
            sb.AppendLine($"  CanAffordUnredact(): {canAffordNow} (expected: false)");
            sb.AppendLine();

            // Restore balance
            MoneyManager.Instance.ResetBalance();

            bool passed = canAfford == expected && !canAffordNow;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests TrySpendForUnredact with sufficient funds
        /// </summary>
        public void TestTrySpendForUnredact()
        {
            if (!ValidateComponents()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY CONTROLLER TESTER: TrySpendForUnredact ===");
            sb.AppendLine();

            // Reset to known state
            MoneyManager.Instance.ResetBalance();

            int beforeBalance = MoneyController.Instance.CurrentBalance;
            int unredactCost = MoneyController.Instance.UnredactCost;
            int expectedAfter = beforeBalance - unredactCost;

            sb.AppendLine($"  Balance before: ${beforeBalance}");
            sb.AppendLine($"  Unredact cost: ${unredactCost}");
            sb.AppendLine($"  Expected after: ${expectedAfter}");
            sb.AppendLine();

            // Reset event tracking
            balanceChangedEventReceived = false;
            lastBalanceFromEvent = -1;
            spendFailedEventReceived = false;

            // Try to spend
            bool result = MoneyController.Instance.TrySpendForUnredact();

            int afterBalance = MoneyController.Instance.CurrentBalance;

            sb.AppendLine("--- AFTER SPEND ---");
            sb.AppendLine($"  TrySpendForUnredact returned: {result}");
            sb.AppendLine($"  Balance after: ${afterBalance} (expected: ${expectedAfter})");
            sb.AppendLine($"  OnBalanceChanged fired: {balanceChangedEventReceived}");
            sb.AppendLine($"  OnSpendFailed fired: {spendFailedEventReceived} (expected: false)");
            sb.AppendLine();

            bool passed = result && afterBalance == expectedAfter && balanceChangedEventReceived && !spendFailedEventReceived;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests TrySpendForUnredact with insufficient funds
        /// </summary>
        public void TestTrySpendForUnredactInsufficient()
        {
            if (!ValidateComponents()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY CONTROLLER TESTER: TrySpendForUnredact (Insufficient) ===");
            sb.AppendLine();

            // Drain balance
            MoneyManager.Instance.ResetBalance();
            int unredactCost = MoneyController.Instance.UnredactCost;
            while (MoneyManager.Instance.CurrentBalance >= unredactCost)
            {
                MoneyManager.Instance.Spend(unredactCost);
            }

            int beforeBalance = MoneyController.Instance.CurrentBalance;

            sb.AppendLine($"  Balance before: ${beforeBalance}");
            sb.AppendLine($"  Unredact cost: ${unredactCost}");
            sb.AppendLine();

            // Reset event tracking
            balanceChangedEventReceived = false;
            spendFailedEventReceived = false;
            lastFailedCost = -1;

            // Try to spend
            bool result = MoneyController.Instance.TrySpendForUnredact();

            int afterBalance = MoneyController.Instance.CurrentBalance;

            sb.AppendLine("--- AFTER SPEND ATTEMPT ---");
            sb.AppendLine($"  TrySpendForUnredact returned: {result} (expected: false)");
            sb.AppendLine($"  Balance after: ${afterBalance} (expected: ${beforeBalance})");
            sb.AppendLine($"  OnBalanceChanged fired: {balanceChangedEventReceived} (expected: false)");
            sb.AppendLine($"  OnSpendFailed fired: {spendFailedEventReceived} (expected: true)");
            sb.AppendLine($"  Failed cost: ${lastFailedCost} (expected: ${unredactCost})");
            sb.AppendLine();

            bool passed = !result && afterBalance == beforeBalance && !balanceChangedEventReceived && spendFailedEventReceived && lastFailedCost == unredactCost;

            // Restore balance (after calculating pass/fail to avoid event interference)
            MoneyManager.Instance.ResetBalance();
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests TrySpend method
        /// </summary>
        public void TestTrySpend()
        {
            if (!ValidateComponents()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY CONTROLLER TESTER: TrySpend ===");
            sb.AppendLine();

            // Reset to known state
            MoneyManager.Instance.ResetBalance();

            int testCost = 75;
            int beforeBalance = MoneyController.Instance.CurrentBalance;
            int expectedAfter = beforeBalance - testCost;

            sb.AppendLine($"  Balance before: ${beforeBalance}");
            sb.AppendLine($"  Test cost: ${testCost}");
            sb.AppendLine();

            // Reset event tracking
            balanceChangedEventReceived = false;
            spendFailedEventReceived = false;

            // Try to spend
            bool result = MoneyController.Instance.TrySpend(testCost);

            int afterBalance = MoneyController.Instance.CurrentBalance;

            sb.AppendLine("--- AFTER SPEND ---");
            sb.AppendLine($"  TrySpend returned: {result}");
            sb.AppendLine($"  Balance after: ${afterBalance} (expected: ${expectedAfter})");
            sb.AppendLine($"  OnBalanceChanged fired: {balanceChangedEventReceived}");
            sb.AppendLine($"  OnSpendFailed fired: {spendFailedEventReceived} (expected: false)");
            sb.AppendLine();

            bool passed = result && afterBalance == expectedAfter && balanceChangedEventReceived && !spendFailedEventReceived;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests AddMoney through controller
        /// </summary>
        public void TestAddMoney()
        {
            if (!ValidateComponents()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY CONTROLLER TESTER: AddMoney ===");
            sb.AppendLine();

            int addAmount = 200;
            int beforeBalance = MoneyController.Instance.CurrentBalance;
            int expectedAfter = beforeBalance + addAmount;

            sb.AppendLine($"  Balance before: ${beforeBalance}");
            sb.AppendLine($"  Adding: ${addAmount}");
            sb.AppendLine();

            // Reset event tracking
            balanceChangedEventReceived = false;
            lastBalanceFromEvent = -1;

            // Add money
            bool result = MoneyController.Instance.AddMoney(addAmount);

            int afterBalance = MoneyController.Instance.CurrentBalance;

            sb.AppendLine("--- AFTER ADD ---");
            sb.AppendLine($"  AddMoney returned: {result}");
            sb.AppendLine($"  Balance after: ${afterBalance} (expected: ${expectedAfter})");
            sb.AppendLine($"  OnBalanceChanged fired: {balanceChangedEventReceived}");
            sb.AppendLine($"  Event balance: ${lastBalanceFromEvent}");
            sb.AppendLine();

            bool passed = result && afterBalance == expectedAfter && balanceChangedEventReceived && lastBalanceFromEvent == expectedAfter;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Logs current state
        /// </summary>
        public void LogCurrentState()
        {
            if (!ValidateComponents()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY CONTROLLER STATE ===");
            sb.AppendLine();

            sb.AppendLine("--- CONFIGURATION ---");
            sb.AppendLine($"  Unredact Cost: ${MoneyController.Instance.UnredactCost}");
            sb.AppendLine();

            sb.AppendLine("--- BALANCE ---");
            sb.AppendLine($"  Current Balance: {MoneyController.Instance.GetFormattedBalance()}");
            sb.AppendLine($"  Can Afford Unredact: {MoneyController.Instance.CanAffordUnredact()}");
            sb.AppendLine();

            sb.AppendLine("=== END STATE ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Resets balance via manager
        /// </summary>
        public void ResetBalance()
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.ResetBalance();
                Debug.Log($"MoneyControllerTester: Balance reset to ${MoneyManager.Instance.CurrentBalance}");
            }
        }

        private bool ValidateComponents()
        {
            if (MoneyManager.Instance == null)
            {
                Debug.LogError("MoneyControllerTester: MoneyManager not found!");
                return false;
            }

            if (MoneyController.Instance == null)
            {
                Debug.LogError("MoneyControllerTester: MoneyController not found!");
                return false;
            }

            return true;
        }
    }
}
