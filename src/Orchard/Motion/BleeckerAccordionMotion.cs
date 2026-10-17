namespace Orchard;

internal static class BleeckerAccordionMotion
{
    internal static double RevealHeight(double naturalHeight,double fraction)=>Math.Floor(naturalHeight*fraction*64)/64;
    internal static double Expo(double progress)
    {
        if(progress<=0)return 0;if(progress>=1)return 1;double low=0,high=1;
        for(var index=0;index<28;index++){var parameter=(low+high)/2;var x=3*(1-parameter)*(1-parameter)*parameter*.16+3*(1-parameter)*parameter*parameter*.3+parameter*parameter*parameter;if(x<progress)low=parameter;else high=parameter;}
        return 1-Math.Pow(1-(low+high)/2,3);
    }
}
