using System;
using System.Linq;
using System.Reflection;
namespace NUnit.Framework {
  [AttributeUsage(AttributeTargets.Class)] public class TestFixtureAttribute : Attribute {}
  [AttributeUsage(AttributeTargets.Method)] public class TestAttribute : Attribute {}
  public class AssertionException : Exception { public AssertionException(string m):base(m){} }
  public static class Assert {
    static void F(string m){ throw new AssertionException(m); }
    public static void IsTrue(bool c, string m=null){ if(!c) F("IsTrue failed "+m); }
    public static void IsFalse(bool c, string m=null){ if(c) F("IsFalse failed "+m); }
    public static void AreEqual(int e,int a,string m=null){ if(e!=a) F($"Expected {e} got {a} {m}"); }
    public static void AreEqual(object e,object a,string m=null){ if(!Equals(e,a)) F($"Expected {e} got {a} {m}"); }
    public static void AreEqual(float e,float a,float d,string m=null){ if(Math.Abs(e-a)>d) F($"Expected {e} got {a} {m}"); }
    public static void AreNotEqual(object e,object a,string m=null){ if(Equals(e,a)) F($"Expected not {e} {m}"); }
    public static void AreNotEqual(int e,int a,string m=null){ if(e==a) F($"Expected not {e} {m}"); }
    public static void Greater(int a,int b,string m=null){ if(!(a>b)) F($"{a} !> {b} {m}"); }
    public static void Greater(float a,float b,string m=null){ if(!(a>b)) F($"{a} !> {b} {m}"); }
    public static void Less(int a,int b,string m=null){ if(!(a<b)) F($"{a} !< {b} {m}"); }
    public static void Less(float a,float b,string m=null){ if(!(a<b)) F($"{a} !< {b} {m}"); }
    public static void IsNull(object o,string m=null){ if(o!=null) F("expected null "+m); }
    public static void IsNotNull(object o,string m=null){ if(o==null) F("expected not null "+m); }
    public static void GreaterOrEqual(int a,int b,string m=null){ if(!(a>=b)) F($"{a} !>= {b} {m}"); }
    public static void GreaterOrEqual(float a,float b,string m=null){ if(!(a>=b)) F($"{a} !>= {b} {m}"); }
  }
}
public static class Runner {
  public static int Main(){
    int pass=0, fail=0;
    foreach(var t in Assembly.GetExecutingAssembly().GetTypes()){
      foreach(var m in t.GetMethods().Where(x=>x.GetCustomAttribute<NUnit.Framework.TestAttribute>()!=null)){
        var o=Activator.CreateInstance(t);
        try{ m.Invoke(o,null); pass++; Console.WriteLine("PASS "+t.Name+"."+m.Name);}
        catch(TargetInvocationException e){ fail++; Console.WriteLine("FAIL "+t.Name+"."+m.Name+": "+e.InnerException.Message);}
      }
    }
    Console.WriteLine($"passed {pass}, failed {fail}");
    return fail==0?0:1;
  }
}
