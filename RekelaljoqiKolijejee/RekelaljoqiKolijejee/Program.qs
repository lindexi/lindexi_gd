namespace Quantum.RekelaljoqiKolijejee 
{
	open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Convert;

    @EntryPoint()
    operation GenerateRandom() : Unit
    {
	    mutable r = GenerateQuantumRandom();
        mutable b = ResultAsBool(r);
        Message(BoolAsString(b));
	}

    operation GenerateQuantumRandom() : Result
    {
        using(q = Qubit())
        {
			H(q);
            return MResetZ(q);
		}
	}
}               