// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

internal record AssemblyCommandsGeneratingModel
{
    public required string Namespace { get; init; }

    public required object AssemblyCommandHandlerType { get; init; }

/*
   生成代码如下：
   [NullableContext(1)]
   [Nullable(0)]
   [RequiredMember]
   internal class AssemblyCommandsGeneratingModel : 
   /*[Nullable(0)]* /
   IEquatable<AssemblyCommandsGeneratingModel>
   {
     [CompilerGenerated]
     [DebuggerBrowsable(DebuggerBrowsableState.Never)]
     private readonly string <Namespace>k__BackingField;
     [CompilerGenerated]
     [DebuggerBrowsable(DebuggerBrowsableState.Never)]
     private readonly object <AssemblyCommandHandlerType>k__BackingField;
   
     [CompilerGenerated]
     protected virtual Type EqualityContract
     {
       [CompilerGenerated] get
       {
         return typeof (AssemblyCommandsGeneratingModel);
       }
     }
   
     [RequiredMember]
     public string Namespace
     {
       [CompilerGenerated] get
       {
         return this.<Namespace>k__BackingField;
       }
       [CompilerGenerated] init
       {
         this.<Namespace>k__BackingField = value;
       }
     }
   
     [RequiredMember]
     public object AssemblyCommandHandlerType
     {
       [CompilerGenerated] get
       {
         return this.<AssemblyCommandHandlerType>k__BackingField;
       }
       [CompilerGenerated] init
       {
         this.<AssemblyCommandHandlerType>k__BackingField = value;
       }
     }
   
     [CompilerGenerated]
     public override string ToString()
     {
       StringBuilder builder = new StringBuilder();
       builder.Append("AssemblyCommandsGeneratingModel");
       builder.Append(" { ");
       if (this.PrintMembers(builder))
         builder.Append(' ');
       builder.Append('}');
       return builder.ToString();
     }
   
     [CompilerGenerated]
     protected virtual bool PrintMembers(StringBuilder builder)
     {
       RuntimeHelpers.EnsureSufficientExecutionStack();
       builder.Append("Namespace = ");
       builder.Append((object) this.Namespace);
       builder.Append(", AssemblyCommandHandlerType = ");
       builder.Append(this.AssemblyCommandHandlerType);
       return true;
     }
   
     [NullableContext(2)]
     [CompilerGenerated]
     [SpecialName]
     public static bool op_Inequality(
       AssemblyCommandsGeneratingModel left,
       AssemblyCommandsGeneratingModel right)
     {
       return !AssemblyCommandsGeneratingModel.op_Equality(left, right);
     }
   
     [NullableContext(2)]
     [CompilerGenerated]
     [SpecialName]
     public static bool op_Equality(
       AssemblyCommandsGeneratingModel left,
       AssemblyCommandsGeneratingModel right)
     {
       if ((object) left == (object) right)
         return true;
       return (object) left != null && left.Equals(right);
     }
   
     [CompilerGenerated]
     public override int GetHashCode()
     {
       return (EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<Namespace>k__BackingField)) * -1521134295 + EqualityComparer<object>.Default.GetHashCode(this.<AssemblyCommandHandlerType>k__BackingField);
     }
   
     [NullableContext(2)]
     [CompilerGenerated]
     public override bool Equals(object obj)
     {
       return this.Equals(obj as AssemblyCommandsGeneratingModel);
     }
   
     [NullableContext(2)]
     [CompilerGenerated]
     public virtual bool Equals(AssemblyCommandsGeneratingModel other)
     {
       if ((object) this == (object) other)
         return true;
       return (object) other != null && Type.op_Equality(this.EqualityContract, other.EqualityContract) && EqualityComparer<string>.Default.Equals(this.<Namespace>k__BackingField, other.<Namespace>k__BackingField) && EqualityComparer<object>.Default.Equals(this.<AssemblyCommandHandlerType>k__BackingField, other.<AssemblyCommandHandlerType>k__BackingField);
     }
   
     [CompilerGenerated]
     public virtual AssemblyCommandsGeneratingModel <Clone>$()
     {
       return new AssemblyCommandsGeneratingModel(this);
     }
   
     [CompilerGenerated]
     [SetsRequiredMembers]
     protected AssemblyCommandsGeneratingModel(AssemblyCommandsGeneratingModel original)
     {
       base..ctor();
       this.<Namespace>k__BackingField = original.<Namespace>k__BackingField;
       this.<AssemblyCommandHandlerType>k__BackingField = original.<AssemblyCommandHandlerType>k__BackingField;
     }
   
     [Obsolete("Constructors of types with required members are not supported in this version of your compiler.", true)]
     [CompilerFeatureRequired("RequiredMembers")]
     public AssemblyCommandsGeneratingModel()
     {
       base..ctor();
     }
   }
   
 */
}