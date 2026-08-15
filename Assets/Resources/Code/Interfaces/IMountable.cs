using UnityEngine;

public interface IMountable
{

    void AddDownStream(GameObject mountable);
    void AddUpstream(GameObject mountable);
    void AddBidirectional(GameObject mountable);

}
