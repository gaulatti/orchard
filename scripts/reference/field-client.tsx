import React, {useState} from 'react';
import {createRoot} from 'react-dom/client';
import {Field} from 'frozen/field';
import {Input} from 'frozen/input';
import {Button} from 'frozen/button';

const root = createRoot(document.getElementById('root')!);
function View({spec}) {
  const [value, setValue] = useState(spec.value ?? 'Consumer text');
  const action = spec.action === 'button' ? <Button variant='link' onClick={() => window.fieldEvents.push('action')}>Reset</Button> : spec.action;
  const label = spec.rich ? <span>Coverage <strong>details</strong></span> : spec.label ?? 'Coverage title';
  return <Field data-field-fixture label={label} required={spec.required} optional={spec.optional} action={action}
    description={spec.description} error={spec.error}>
    <Input id={spec.controlId} aria-describedby={spec.describedBy} aria-invalid={spec.invalid} aria-label={spec.childName}
      required={spec.childRequired} disabled={spec.disabled} value={value} onChange={event => {setValue(event.target.value); window.fieldEvents.push(event.target.value);}} />
  </Field>;
}
window.fieldEvents = [];
window.showField = spec => {document.getElementById('root')!.style.width = (spec.width ?? 320) + 'px'; root.render(<View key={spec.key ?? spec.id} spec={spec}/>);};
