namespace ChurchApp.Models.Finance
{
    public enum AccountCurrency
    {
        NGN = 1,
        USD = 2,
        GBP = 3,
        EUR = 4
    }

    public enum IncomeAccountSelectionMode
    {
        FixedAccount = 1,
        AuthorisedSelection = 2
    }

    public enum RemittanceCalculationBasis
    {
        PercentageOfIncome = 1,
        FixedAmount = 2,
        NotApplicable = 3
    }
}