public interface IFluidReceiver
{
    float GetRemainingCapacityLitersThisDT(float dt);
    float ReceiveFluid(float amountL, float dt);

}