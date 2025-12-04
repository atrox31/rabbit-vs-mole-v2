public enum PlayerControlAgent
{
    None,   // Brak spawnu danego rodzaju gracza
    Human,  // Spawn gracza oraz przypisanie odpowiedniego inputu dla sterowania
    AI,     // Spawnowany gracz AI (osobny prefab sterowany przez agenta)
    Online  // Spawnowany gracz online (zależnie od wyboru)
}

