namespace HardwareVault.Core.Mapping
{
    public static class OsMappings
    {
        public static string GetProductTypeName(int value) => value switch
        {
            0 => "Unknown",
            1 => "Workstation",
            2 => "Domain Controller",
            3 => "Server",
            _ => "Unknown"
        };

        public static string GetOsSkuName(int value) => value switch
        {
            0 => "Undefined",
            1 => "Ultimate Edition",
            2 => "Home Basic Edition",
            3 => "Home Premium Edition",
            4 => "Enterprise Edition",
            6 => "Business Edition",
            7 => "Standard Server Edition (Desktop Experience)",
            8 => "Datacenter Server Edition (Desktop Experience)",
            9 => "Small Business Server Edition",
            10 => "Enterprise Server Edition",
            11 => "Starter Edition",
            12 => "Datacenter Server Core Edition",
            13 => "Standard Server Core Edition",
            14 => "Enterprise Server Core Edition",
            17 => "Web Server Edition",
            19 => "Home Server Edition",
            20 => "Storage Express Server Edition",
            21 => "Storage Standard Server Edition (Desktop Experience)",
            22 => "Storage Workgroup Server Edition (Desktop Experience)",
            23 => "Storage Enterprise Server Edition",
            24 => "Server For Small Business Edition",
            25 => "Small Business Server Premium Edition",
            27 => "Windows Enterprise Edition",
            28 => "Windows Ultimate Edition",
            29 => "Web Server Edition (Server Core)",
            36 => "Server Standard Edition without Hyper-V",
            37 => "Datacenter Edition without Hyper-V",
            38 => "Enterprise Edition without Hyper-V",
            39 => "Datacenter Core Edition without Hyper-V",
            40 => "Standard Core Edition without Hyper-V",
            41 => "Enterprise Core Edition without Hyper-V",
            42 => "Microsoft Hyper-V Server",
            43 => "Storage Express Edition (Server Core)",
            44 => "Storage Standard Edition (Server Core)",
            45 => "Storage Workgroup Edition (Server Core)",
            46 => "Storage Enterprise Edition (Server Core)",
            48 => "Windows Professional",
            50 => "Windows Server Essentials (Desktop Experience)",
            63 => "Small Business Server Premium (Server Core)",
            64 => "Compute Cluster Server without Hyper-V",
            97 => "Windows RT",
            101 => "Windows Home",
            103 => "Windows Professional with Media Center",
            104 => "Windows Mobile",
            123 => "Windows IoT Core",
            143 => "Datacenter Edition (Nano Server)",
            144 => "Standard Edition (Nano Server)",
            147 => "Datacenter Edition (Server Core)",
            148 => "Standard Edition (Server Core)",
            175 => "Enterprise for Virtual Desktops",
            _ => "Unknown"
        };
    }
}