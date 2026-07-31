public interface IFluidReceiver
{
    float GetRemainingCapacityLitersThisDT(float dt, FluidType fluid);
    float ReceiveFluid(float amountL, float dt, FluidType type);

}