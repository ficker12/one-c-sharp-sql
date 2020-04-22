DECLARE @doc_date AS DATETIME = '4015-04-25';
DECLARE @doc_number AS NVARCHAR (11) = N'00-00000022';

SELECT
	N'jcfg:DocumentObject.������������������������' AS [#type],
	dbo.fn_sql_to_1c_uuid(H._IDRRef)                AS [#value.Ref],           -- ������
	CAST(H._Marked AS bit)                          AS [#value.DeletionMark],  -- ������
	CONVERT(nvarchar(19), H._Date_Time, 126)        AS [#value.Date],          -- ����
	H._Number                                       AS [#value.Number],        -- ������
	CAST(_Posted AS bit)                            AS [#value.Posted],        -- ������
	dbo.fn_sql_to_1c_uuid(H._Fld28871RRef)          AS [#value.�������������], -- ��������� ���
	H._Fld28872                                     AS [#value.�����������],   -- ������
	CAST(H._Fld28873 AS bit)                        AS [#value.����������],    -- ������

	-- ����������������� (��������� ��� ������)
	CASE WHEN H._Fld28874_RTRef = 0x00000243
	     THEN N'jcfg:DocumentRef.���������������'      -- filed _Fld28874_TYPE is not used !
		 ELSE NULL END                              AS [#value.�����������������.#type],  -- ��� ���� �������
	dbo.fn_sql_to_1c_uuid(H._Fld28874_RRRef)        AS [#value.�����������������.#value], -- ������ �������

	dbo.fn_sql_to_1c_uuid(H._Fld28875RRef)          AS [#value.������], -- �������� ������������"����������"

	-- ��������� ����� "������" : ���� [_Fld1551], [_KeyField], [_LineNo28877]
	(SELECT
			dbo.fn_sql_to_1c_uuid(T._Fld28878RRef) AS ������������,
			dbo.fn_sql_to_1c_uuid(T._Fld28879RRef) AS ��������������,
			dbo.fn_sql_to_1c_uuid(T._Fld28880RRef) AS ��������,
			dbo.fn_sql_to_1c_uuid(T._Fld28881RRef) AS �������,
			T._Fld28882                            AS ����,
			CAST(T._Fld28883 AS bit)               AS �������������������
	FROM _Document840_VT28876 AS T
	WHERE T._Document840_IDRRef = H._IDRRef
	ORDER BY T._Document840_IDRRef, T._KeyField FOR JSON AUTO) AS [#value.������]

FROM _Document840 AS H -- ��������.������������������������
WHERE H._Date_Time = @doc_date AND H._Number = @doc_number
FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES;

