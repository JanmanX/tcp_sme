using System;
using SME;

// XXX: Why does it throw an error when namespacing SME.Components???
namespace TCPIP
{
//    [ClockedProcess]
    public class EightPortMemory<T> : SimpleProcess
    {
        /// <summary>
        /// The controller bus for port A
        /// </summary>
        public interface IControlA : IBus
        {
            [InitialValue]
            bool IsWriting { get; set; }
            [InitialValue]
            bool Enabled { get; set; }
            [InitialValue]
            int Address { get; set; }
            T Data { get; set; }
        }

       public interface IControlB : IBus
        {
            [InitialValue]
            bool IsWriting { get; set; }
            [InitialValue]
            bool Enabled { get; set; }
            [InitialValue]
            int Address { get; set; }
            T Data { get; set; }
        }
       public interface IControlC : IBus
        {
            [InitialValue]
            bool IsWriting { get; set; }
            [InitialValue]
            bool Enabled { get; set; }
            [InitialValue]
            int Address { get; set; }
            T Data { get; set; }
        }
       public interface IControlD : IBus
        {
            [InitialValue]
            bool IsWriting { get; set; }
            [InitialValue]
            bool Enabled { get; set; }
            [InitialValue]
            int Address { get; set; }
            T Data { get; set; }
        }
       public interface IControlE : IBus
        {
            [InitialValue]
            bool IsWriting { get; set; }
            [InitialValue]
            bool Enabled { get; set; }
            [InitialValue]
            int Address { get; set; }
            T Data { get; set; }
        }
       public interface IControlF : IBus
        {
            [InitialValue]
            bool IsWriting { get; set; }
            [InitialValue]
            bool Enabled { get; set; }
            [InitialValue]
            int Address { get; set; }
            T Data { get; set; }
        }
       public interface IControlG : IBus
        {
            [InitialValue]
            bool IsWriting { get; set; }
            [InitialValue]
            bool Enabled { get; set; }
            [InitialValue]
            int Address { get; set; }
            T Data { get; set; }
        }
       public interface IControlH : IBus
        {
            [InitialValue]
            bool IsWriting { get; set; }
            [InitialValue]
            bool Enabled { get; set; }
            [InitialValue]
            int Address { get; set; }
            T Data { get; set; }
        }


        public interface IReadResultA : IBus
        {
            T Data { get; set; }
        }
        public interface IReadResultB : IBus
        {
            T Data { get; set; }
        }
        public interface IReadResultC : IBus
        {
            T Data { get; set; }
        }
        public interface IReadResultD : IBus
        {
            T Data { get; set; }
        }
        public interface IReadResultE : IBus
        {
            T Data { get; set; }
        }
        public interface IReadResultF : IBus
        {
            T Data { get; set; }
        }
        public interface IReadResultG : IBus
        {
            T Data { get; set; }
        }
        public interface IReadResultH : IBus
        {
            T Data { get; set; }
        }


 //       [InputBus]
 //       public readonly IControlA ControlA = Scope.CreateBus<IControlA>();
        [InputBus]
        public readonly IControlB ControlB = Scope.CreateBus<IControlB>();
//        [InputBus]
//        public readonly IControlC ControlC = Scope.CreateBus<IControlC>();
//        [InputBus]
//        public readonly IControlD ControlD = Scope.CreateBus<IControlD>();
//        [InputBus]
//        public readonly IControlE ControlE = Scope.CreateBus<IControlE>();
//        [InputBus]
//        public readonly IControlF ControlF = Scope.CreateBus<IControlF>();
//        [InputBus]
//        public readonly IControlG ControlG = Scope.CreateBus<IControlG>();
//        [InputBus]
//        public readonly IControlH ControlH = Scope.CreateBus<IControlH>();


        [OutputBus]
        public readonly IReadResultA ReadResultA = Scope.CreateBus<IReadResultA>();
        [OutputBus]
        public readonly IReadResultB ReadResultB = Scope.CreateBus<IReadResultB>();
        [OutputBus]
        public readonly IReadResultC ReadResultC = Scope.CreateBus<IReadResultC>();
        [OutputBus]
        public readonly IReadResultD ReadResultD = Scope.CreateBus<IReadResultD>();
        [OutputBus]
        public readonly IReadResultE ReadResultE = Scope.CreateBus<IReadResultE>();
        [OutputBus]
        public readonly IReadResultF ReadResultF = Scope.CreateBus<IReadResultF>();
        [OutputBus]
        public readonly IReadResultG ReadResultG = Scope.CreateBus<IReadResultG>();
        [OutputBus]
        public readonly IReadResultH ReadResultH = Scope.CreateBus<IReadResultH>();



        /// <summary>
        /// The stored memory
        /// </summary>
        [Signal]
        private readonly T[] m_memory;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:SME.Components.TrueDualPortMemory`1"/> class.
        /// </summary>
        /// <param name="size">The size of the allocated memory area.</param>
        /// <param name="initial">The initial memory contents (optional).</param>
        public EightPortMemory(int size, T[] initial = null)
        {
            m_memory = new T[size];
            if (initial != null) {
                Array.Copy(initial, 0, m_memory, 0, Math.Min(initial.Length, size));                        
            }

            SimulationOnly(() =>
            {
                Console.WriteLine("Partial implementation of EightPortMemory!\nRead/Write on same addresses not checked!");
	
            });
        }

        /// <summary>
        /// Performs the operations when the signals are ready
        /// </summary>
        protected override void OnTick()
        {
 /* 
            SimulationOnly(() =>
            {
                if (ControlA.Enabled && ControlB.Enabled && ControlA.Address == ControlB.Address)
                {
                    if (ControlA.IsWriting && ControlB.IsWriting)
                        throw new Exception("Both ports are writing the same memory address");

                    if (ControlA.IsWriting == !ControlB.IsWriting)
                        throw new Exception("Conflicting read and write to the same memory address");
                }
            });

            if (ControlA.Enabled)
            {
                ReadResultA.Data = m_memory[ControlA.Address];
                if (ControlA.IsWriting)
                    m_memory[ControlA.Address] = ControlA.Data;
            }
*/
            if (ControlB.Enabled)
            {
                ReadResultB.Data = m_memory[ControlB.Address];
                if (ControlB.IsWriting)
                    m_memory[ControlB.Address] = ControlB.Data;
            }

            /* 
            if (ControlC.Enabled)
            {
                ReadResultC.Data = m_memory[ControlC.Address];
                if (ControlC.IsWriting)
                    m_memory[ControlC.Address] = ControlC.Data;
            }

            if (ControlD.Enabled)
            {
                ReadResultD.Data = m_memory[ControlD.Address];
                if (ControlD.IsWriting)
                    m_memory[ControlD.Address] = ControlD.Data;
            }
            if (ControlE.Enabled)
            {
                ReadResultE.Data = m_memory[ControlE.Address];
                if (ControlE.IsWriting)
                    m_memory[ControlE.Address] = ControlE.Data;
            }

            if (ControlF.Enabled)
            {
                ReadResultF.Data = m_memory[ControlF.Address];
                if (ControlF.IsWriting)
                    m_memory[ControlF.Address] = ControlF.Data;
            }
            if (ControlG.Enabled)
            {
                ReadResultG.Data = m_memory[ControlG.Address];
                if (ControlG.IsWriting)
                    m_memory[ControlG.Address] = ControlG.Data;
            }

            if (ControlH.Enabled)
            {
                ReadResultH.Data = m_memory[ControlH.Address];
                if (ControlH.IsWriting)
                    m_memory[ControlH.Address] = ControlH.Data;
            }
            */
        }
    }
}
