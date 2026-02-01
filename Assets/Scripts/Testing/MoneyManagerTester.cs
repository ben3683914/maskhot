using UnityEngine;
using System.Text;
using Maskhot.Managers;

namespace Maskhot.Testing
{
    /// <summary>
    /// Test script to verify MoneyManager functionality.
    ///
    /// Setup:
    /// 1. Attach to a GameObject alongside MoneyManager
    /// 2. Enter Play Mode
    ///
    /// How to Run: Click the buttons in the Inspector (enabled during Play Mode)
    ///
    /// What to Verify:
    /// - CurrentBalance starts at startingBalance
    /// - CanAfford returns correct values
    /// - Spend deducts money and fires event
    /// - AddMoney increases balance and fires event
    /// - ResetBalance restores starting amount
    /// </summary>
    public class MoneyManagerTester : MonoBehaviour
    {
        [Header("Test Configuration")]
        [Tooltip("Amount to use for spend/add tests")]
        public int testAmount = 100;

        [Header("Options")]
        [Tooltip("Enable detailed logging")]
        public bool verboseOutput = false;

        private bool balanceChangedEventReceived = false;
        private int lastBalanceFromEvent = -1;

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
            // Re-subscribe in case manager wasn't ready
            UnsubscribeFromEvents();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.OnBalanceChanged += HandleBalanceChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.OnBalanceChanged -= HandleBalanceChanged;
            }
        }

        private void HandleBalanceChanged(int newBalance)
        {
            balanceChangedEventReceived = true;
            lastBalanceFromEvent = newBalance;

            if (verboseOutput)
            {
                Debug.Log($"MoneyManagerTester: OnBalanceChanged fired with balance ${newBalance}");
            }
        }

        /// <summary>
        /// Tests initial balance state
        /// </summary>
        public void TestInitialState()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY MANAGER TESTER: Initial State ===");
            sb.AppendLine();

            sb.AppendLine("--- CONFIGURATION ---");
            sb.AppendLine($"  Starting Balance: ${MoneyManager.Instance.StartingBalance}");
            sb.AppendLine();

            sb.AppendLine("--- CURRENT STATE ---");
            sb.AppendLine($"  Current Balance: ${MoneyManager.Instance.CurrentBalance}");
            sb.AppendLine();

            bool passed = MoneyManager.Instance.CurrentBalance == MoneyManager.Instance.StartingBalance;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "INFO")} ---");
            if (!passed)
            {
                sb.AppendLine("  Note: Balance may differ from starting if transactions have occurred");
            }
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests CanAfford method
        /// </summary>
        public void TestCanAfford()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY MANAGER TESTER: CanAfford ===");
            sb.AppendLine();

            int balance = MoneyManager.Instance.CurrentBalance;
            sb.AppendLine($"  Current Balance: ${balance}");
            sb.AppendLine();

            sb.AppendLine("--- TESTS ---");

            // Test exact amount
            bool canAffordExact = MoneyManager.Instance.CanAfford(balance);
            sb.AppendLine($"  CanAfford(${balance}) [exact]: {canAffordExact} (expected: true)");

            // Test less than balance
            int lessThan = balance > 0 ? balance - 1 : 0;
            bool canAffordLess = MoneyManager.Instance.CanAfford(lessThan);
            sb.AppendLine($"  CanAfford(${lessThan}) [less]: {canAffordLess} (expected: true)");

            // Test more than balance
            int moreThan = balance + 1;
            bool canAffordMore = MoneyManager.Instance.CanAfford(moreThan);
            sb.AppendLine($"  CanAfford(${moreThan}) [more]: {canAffordMore} (expected: false)");

            // Test zero
            bool canAffordZero = MoneyManager.Instance.CanAfford(0);
            sb.AppendLine($"  CanAfford($0) [zero]: {canAffordZero} (expected: true)");

            // Test negative
            bool canAffordNegative = MoneyManager.Instance.CanAfford(-10);
            sb.AppendLine($"  CanAfford(-$10) [negative]: {canAffordNegative} (expected: false)");
            sb.AppendLine();

            bool passed = canAffordExact && canAffordLess && !canAffordMore && canAffordZero && !canAffordNegative;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests Spend method
        /// </summary>
        public void TestSpend()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY MANAGER TESTER: Spend ===");
            sb.AppendLine();

            // Reset to known state
            MoneyManager.Instance.ResetBalance();

            int beforeBalance = MoneyManager.Instance.CurrentBalance;
            sb.AppendLine($"  Balance before: ${beforeBalance}");
            sb.AppendLine($"  Test amount: ${testAmount}");
            sb.AppendLine();

            // Reset event tracking
            balanceChangedEventReceived = false;
            lastBalanceFromEvent = -1;

            // Spend
            bool spendResult = MoneyManager.Instance.Spend(testAmount);

            int afterBalance = MoneyManager.Instance.CurrentBalance;
            int expectedBalance = beforeBalance - testAmount;

            sb.AppendLine("--- AFTER SPEND ---");
            sb.AppendLine($"  Spend returned: {spendResult}");
            sb.AppendLine($"  Balance after: ${afterBalance} (expected: ${expectedBalance})");
            sb.AppendLine($"  Event fired: {balanceChangedEventReceived}");
            sb.AppendLine($"  Event balance: ${lastBalanceFromEvent}");
            sb.AppendLine();

            bool passed = spendResult && afterBalance == expectedBalance && balanceChangedEventReceived && lastBalanceFromEvent == expectedBalance;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests Spend with insufficient funds
        /// </summary>
        public void TestSpendInsufficientFunds()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY MANAGER TESTER: Spend (Insufficient Funds) ===");
            sb.AppendLine();

            int beforeBalance = MoneyManager.Instance.CurrentBalance;
            int tooMuch = beforeBalance + 100;

            sb.AppendLine($"  Balance before: ${beforeBalance}");
            sb.AppendLine($"  Attempting to spend: ${tooMuch}");
            sb.AppendLine();

            // Reset event tracking
            balanceChangedEventReceived = false;

            // Try to spend more than we have
            bool spendResult = MoneyManager.Instance.Spend(tooMuch);

            int afterBalance = MoneyManager.Instance.CurrentBalance;

            sb.AppendLine("--- AFTER SPEND ATTEMPT ---");
            sb.AppendLine($"  Spend returned: {spendResult} (expected: false)");
            sb.AppendLine($"  Balance after: ${afterBalance} (expected: ${beforeBalance})");
            sb.AppendLine($"  Event fired: {balanceChangedEventReceived} (expected: false)");
            sb.AppendLine();

            bool passed = !spendResult && afterBalance == beforeBalance && !balanceChangedEventReceived;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests AddMoney method
        /// </summary>
        public void TestAddMoney()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY MANAGER TESTER: AddMoney ===");
            sb.AppendLine();

            int beforeBalance = MoneyManager.Instance.CurrentBalance;
            sb.AppendLine($"  Balance before: ${beforeBalance}");
            sb.AppendLine($"  Adding: ${testAmount}");
            sb.AppendLine();

            // Reset event tracking
            balanceChangedEventReceived = false;
            lastBalanceFromEvent = -1;

            // Add money
            bool addResult = MoneyManager.Instance.AddMoney(testAmount);

            int afterBalance = MoneyManager.Instance.CurrentBalance;
            int expectedBalance = beforeBalance + testAmount;

            sb.AppendLine("--- AFTER ADD ---");
            sb.AppendLine($"  AddMoney returned: {addResult}");
            sb.AppendLine($"  Balance after: ${afterBalance} (expected: ${expectedBalance})");
            sb.AppendLine($"  Event fired: {balanceChangedEventReceived}");
            sb.AppendLine($"  Event balance: ${lastBalanceFromEvent}");
            sb.AppendLine();

            bool passed = addResult && afterBalance == expectedBalance && balanceChangedEventReceived && lastBalanceFromEvent == expectedBalance;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Tests ResetBalance method
        /// </summary>
        public void TestResetBalance()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY MANAGER TESTER: ResetBalance ===");
            sb.AppendLine();

            // Modify balance first
            MoneyManager.Instance.Spend(50);
            MoneyManager.Instance.AddMoney(25);

            int beforeReset = MoneyManager.Instance.CurrentBalance;
            int expectedAfterReset = MoneyManager.Instance.StartingBalance;

            sb.AppendLine($"  Balance before reset: ${beforeReset}");
            sb.AppendLine($"  Starting balance: ${expectedAfterReset}");
            sb.AppendLine();

            // Reset event tracking
            balanceChangedEventReceived = false;
            lastBalanceFromEvent = -1;

            // Reset
            MoneyManager.Instance.ResetBalance();

            int afterReset = MoneyManager.Instance.CurrentBalance;

            sb.AppendLine("--- AFTER RESET ---");
            sb.AppendLine($"  Balance after: ${afterReset} (expected: ${expectedAfterReset})");
            sb.AppendLine($"  Event fired: {balanceChangedEventReceived}");
            sb.AppendLine($"  Event balance: ${lastBalanceFromEvent}");
            sb.AppendLine();

            bool passed = afterReset == expectedAfterReset && balanceChangedEventReceived && lastBalanceFromEvent == expectedAfterReset;
            sb.AppendLine($"--- RESULT: {(passed ? "PASSED" : "FAILED")} ---");
            sb.AppendLine();

            sb.AppendLine("=== END TEST ===");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Logs current manager state
        /// </summary>
        public void LogCurrentState()
        {
            if (!ValidateManager()) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== MONEY MANAGER STATE ===");
            sb.AppendLine();

            sb.AppendLine($"  Starting Balance: ${MoneyManager.Instance.StartingBalance}");
            sb.AppendLine($"  Current Balance: ${MoneyManager.Instance.CurrentBalance}");
            sb.AppendLine();

            sb.AppendLine("=== END STATE ===");
            Debug.Log(sb.ToString());
        }

        private bool ValidateManager()
        {
            if (MoneyManager.Instance == null)
            {
                Debug.LogError("MoneyManagerTester: MoneyManager not found!");
                return false;
            }
            return true;
        }
    }
}
