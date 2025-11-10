public static class InputValidator
{
    public static bool CanProcessInput(GameState currentState, bool isProcessingMove, bool inputLocked)
    {
        if (inputLocked)
        {
            return false;
        }
        
        if (isProcessingMove)
        {
            return false;
        }
        
        if (currentState != GameState.Playing)
        {
            return false;
        }
        
        return true;
    }
}

