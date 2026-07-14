public interface IFluidSender
{
    void SendFluid(float amount_to_send_L, float dt);
    void AddDownstream(IFluidReceiver target);
    void SetDownstreamLeakTarget(IFluidReceiver target);
    void RemoveDownstreamLeakTarget(IFluidReceiver target);
}