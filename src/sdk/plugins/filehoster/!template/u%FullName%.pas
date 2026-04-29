unit u%FullName%;

interface

uses
  // Delphi
  SysUtils,
  // Plugin system
  uPlugInFileHosterClass, uPlugInConst;

type
  T%FullName% = class(TFileHosterPlugIn)
  public
    function GetName: WideString; override; safecall;
    function CheckLink(const AFile: WideString): TLinkInfo; override; safecall;
  end;

implementation

function T%FullName%.GetName: WideString;
begin
  Result := '%FullName%';
end;

function T%FullName%.CheckLink(const AFile: WideString): TLinkInfo;
begin
  Result.Link := AFile;
  Result.Status := csUnknown;
  Result.Size := 0;
  Result.FileName := '';
  Result.Checksum := '';
  Result.ChecksumType := ctMD5;

  { TODO : implement the link check using HTTPManager if required }
  { TODO : fill TLinkInfo with the parsed file name, size and status }
end;

end.
