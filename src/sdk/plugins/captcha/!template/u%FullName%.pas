unit u%FullName%;

interface

uses
  // Delphi
  SysUtils,
  // Plugin system
  uPlugInCAPTCHAClass, uPlugInConst;

type
  T%FullName% = class(TCAPTCHAPlugIn)
  public
    function GetName: WideString; override; safecall;
    function Exec: WordBool; override; safecall;
  end;

implementation

function T%FullName%.GetName: WideString;
begin
  Result := '%FullName%';
end;

function T%FullName%.Exec: WordBool;
begin
  Result := False;

  { TODO : inspect CAPTCHA, CAPTCHAName and CAPTCHAType }
  { TODO : set CAPTCHAResult and Cookies if a solution was found }
end;

end.
