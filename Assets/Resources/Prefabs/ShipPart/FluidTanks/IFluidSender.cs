public interface IFluidSender
{
    float SendFluid(float amount_to_send_L, float dt, FluidType type);
    void AddDownstream(IFluidReceiver target);
    void SetDownstreamLeakTarget(IFluidReceiver target);
    void RemoveDownstreamLeakTarget(IFluidReceiver target);
}