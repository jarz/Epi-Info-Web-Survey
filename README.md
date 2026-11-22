<H2>Epi Info™ Web Survey</H2>

A free and open source tool developed by CDC for the public health community for one-click publishing of Epi Info™ surveys on the web to collect data from a broad audience. Epidemiologists can distribute links to the surveys via emails, blogs, social media and QR codes, and survey participants can anonymously provide secure responses. The anonymous collected data is stored in a centralized database for instantaneous access and analysis.

For additional info, please watch this (post video link here)

Epi Info™ for Web & Cloud [Website](https://www.cdc.gov/epiinfo/cloud.html)

<H3>Needed</H3> 

Relatively decent Windows machine running 10 or 11.  
Git for Windows (free). [Download](https://git-scm.com/download/win)  
Visual Studio Community Edition (free). [Download](https://visualstudio.microsoft.com/vs/community/)   
Access to a MS SQL Server database with write permissions.  

<H3>Building the Project - Summary</H3>

The production projects target .NET Framework 4.8. Install Visual Studio 2022 (or Build Tools) with the .NET desktop and web build workloads, WCF tooling, and the 4.8 targeting pack, then restore NuGet packages before compiling.

When using the multi-repository Epi Info workspace, see [the workspace build instructions](https://github.com/jarz/Epi-Info-Workspace/blob/main/BUILDING.md). Its build runner uses full-framework MSBuild and keeps compiled outputs separate from checked-in binaries. Compilation does not validate deployment, database connectivity, or the missing root web.config.

<H3>Building the Project</H3>

(coming soon) 

Note: The web config is missing from the Epi.Web project. This is most likely because of keys we don’t want out there. I’ll look into that.

=== readme editing work in progress below ===

Epi Info 7 Introduction to Web Survey
https://github.com/Epi-Info/Epi-Info-Web-Survey/assets/18171996/f2484513-bda0-4603-981c-4d08f8db8015

