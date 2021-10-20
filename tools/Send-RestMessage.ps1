$baseUri = "http://localhost:8161"
if (!(Test-Path variable:cred)) {
    $cred = Get-Credential -UserName artemis_admin
}
$body = @"
<servlet>
  <servlet-name>MessageServlet</servlet-name>  
  <servlet-class>org.apache.activemq.web.MessageServlet</servlet-class>
  <load-on-startup>1</load-on-startup>
  <init-param>
     <param-name>topic</param-name>
     <param-value>false</param-value>
  </init-param>
</servlet>
"@
Invoke-WebRequest "$baseUri/api/message/SaveBillInject?type=queue" -Authentication Basic -ContentType 'application/xml' -body $body 