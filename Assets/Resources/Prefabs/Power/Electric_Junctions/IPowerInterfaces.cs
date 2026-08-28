using UnityEngine;

public interface IPowerGenerator
{
    public float GetPowerGeneratedPerDT(float dt);
}

public interface IPowerCapacity
{
    public float GetPowerCapacity();
}

public interface IPowerConsumer
{
    public float PowerNeededPerUsage();

    public float PowerConsumedPerDT(float dt);
   
    public bool PassiveConsumptionOn();
    
    public void SetPassiveConsumptionOn(bool value);
    
}

public interface IPowerNetworked
{
    public Component_PowerNode TryFindNodeWithPower();

    public bool TryGameStartNetworkConnect();

    public void ConnectToNode(Component_PowerNode node);
    
    public void DisconnectFromNode();
    public float GetPowerRadius_M();
}
