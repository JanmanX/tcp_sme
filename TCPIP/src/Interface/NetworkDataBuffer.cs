using System;
using SME;

namespace TCPIP 
{
    partial class  NetworkDataBuffer : SimpleProcess
    {
        [InputBus]
        NetworkDataBufferBus networkDataBufferBus;

        public NetworkDataBuffer(NetworkDataBufferBus networkDataBufferBus) 
        {
            this.networkDataBufferBus = networkDataBufferBus 
            ?? throw new ArgumentNullException(nameof(networkDataBufferBus));
        }

        protected override void OnTick() 
        {
            
        }
    } 
}